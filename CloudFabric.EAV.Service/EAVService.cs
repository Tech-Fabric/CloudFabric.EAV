using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using AutoMapper;

using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Models.ViewModels.Attributes;
using CloudFabric.EAV.Options;
using CloudFabric.EAV.Service.Serialization;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Persistence;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ProjectionDocumentSchemaFactory =
    CloudFabric.EAV.Domain.Projections.EntityInstanceProjection.ProjectionDocumentSchemaFactory;

namespace CloudFabric.EAV.Service;

public class EAVService : IEAVService
{
    private readonly AggregateRepositoryFactory _aggregateRepositoryFactory;

    private readonly IProjectionRepository<AttributeConfigurationProjectionDocument>
        _attributeConfigurationProjectionRepository;

    private readonly AggregateRepository<AttributeConfiguration> _attributeConfigurationRepository;
    private readonly AggregateRepository<CategoryTree> _categoryTreeRepository;

    private readonly IProjectionRepository<EntityConfigurationProjectionDocument>
        _entityConfigurationProjectionRepository;

    private readonly AggregateRepository<EntityConfiguration> _entityConfigurationRepository;

    private readonly EntityInstanceFromDictionaryDeserializer _entityInstanceFromDictionaryDeserializer;

    private readonly EntityInstanceCreateUpdateRequestFromJsonDeserializer
        _entityInstanceCreateUpdateRequestFromJsonDeserializer;
    private readonly AggregateRepository<EntityInstance> _entityInstanceRepository;
    private readonly ILogger<EAVService> _logger;
    private readonly IMapper _mapper;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ProjectionRepositoryFactory _projectionRepositoryFactory;

    private readonly EventUserInfo _userInfo;

    private readonly ElasticSearchQueryOptions _elasticSearchQueryOptions;

    public EAVService(
        ILogger<EAVService> logger,
        IMapper mapper,
        JsonSerializerOptions jsonSerializerOptions,
        AggregateRepositoryFactory aggregateRepositoryFactory,
        ProjectionRepositoryFactory projectionRepositoryFactory,
        EventUserInfo userInfo,
        IOptions<ElasticSearchQueryOptions>? elasticSearchQueryOptions = null
    )
    {
        _logger = logger;
        _mapper = mapper;
        _jsonSerializerOptions = jsonSerializerOptions;

        _aggregateRepositoryFactory = aggregateRepositoryFactory;
        _projectionRepositoryFactory = projectionRepositoryFactory;

        _userInfo = userInfo;

        _elasticSearchQueryOptions = elasticSearchQueryOptions != null
            ? elasticSearchQueryOptions.Value
            : new ElasticSearchQueryOptions();

        _attributeConfigurationRepository = _aggregateRepositoryFactory
            .GetAggregateRepository<AttributeConfiguration>();
        _entityConfigurationRepository = _aggregateRepositoryFactory
            .GetAggregateRepository<EntityConfiguration>();
        _entityInstanceRepository = _aggregateRepositoryFactory
            .GetAggregateRepository<EntityInstance>();
        _categoryTreeRepository = _aggregateRepositoryFactory
            .GetAggregateRepository<CategoryTree>();

        _attributeConfigurationProjectionRepository = _projectionRepositoryFactory
            .GetProjectionRepository<AttributeConfigurationProjectionDocument>();
        _entityConfigurationProjectionRepository = _projectionRepositoryFactory
            .GetProjectionRepository<EntityConfigurationProjectionDocument>();

        _entityInstanceFromDictionaryDeserializer = new EntityInstanceFromDictionaryDeserializer(_mapper);

        _entityInstanceCreateUpdateRequestFromJsonDeserializer =
            new EntityInstanceCreateUpdateRequestFromJsonDeserializer(
                _attributeConfigurationRepository, jsonSerializerOptions
            );
    }

    private void EnsureAttributeMachineNameIsAdded(AttributeConfigurationCreateUpdateRequest attributeRequest)
    {
        if (string.IsNullOrWhiteSpace(attributeRequest.MachineName))
        {
            var machineName =
                attributeRequest.Name
                    .FirstOrDefault(x => x.CultureInfoId == new CultureInfo("en-US").LCID)
                    ?.String
                ?? attributeRequest.Name.First().String;

            // remove spec symbols
            machineName = machineName.Replace(" ", "_");
            var specSymbolsRegex = new Regex("[^\\d\\w_]*", RegexOptions.None, TimeSpan.FromSeconds(2)); // From example https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex.matchtimeout?view=net-7.0
            attributeRequest.MachineName = specSymbolsRegex.Replace(machineName, "").ToLower();
        }
    }

    private async Task<bool> IsAttributeMachineNameUniqueForEntityConfiguration(string machineName,
        EntityConfiguration entityConfiguration, CancellationToken cancellationToken)
    {
        List<Guid> attributesIds = entityConfiguration.Attributes
            .Select(x => x.AttributeConfigurationId)
            .ToList();

        if (!attributesIds.Any())
        {
            return true;
        }

        // create attributes filter
        ProjectionQueryResult<AttributeConfigurationListItemViewModel> attributes =
            await GetAttributesByIds(attributesIds, cancellationToken);

        if (attributes.Records.Any(x => x.Document?.MachineName == machineName))
        {
            return false;
        }

        return true;
    }

    private async Task<bool> CheckAttributesListMachineNameUnique(
        List<EntityAttributeConfigurationCreateUpdateRequest> attributesRequest,
        CancellationToken cancellationToken)
    {
        // validate reference attributes don't have the same machine name
        List<Guid> referenceAttributes = attributesRequest
            .Where(x => x is EntityAttributeConfigurationCreateUpdateReferenceRequest)
            .Select(x => ((EntityAttributeConfigurationCreateUpdateReferenceRequest)x).AttributeConfigurationId)
            .ToList();

        List<string> machineNames = new();
        if (referenceAttributes.Any())
        {
            machineNames = (await GetAttributesByIds(referenceAttributes, cancellationToken))
                .Records
                .Select(x => x.Document?.MachineName!)
                .ToList();
        }

        // validate new attributes don't have the same machine name
        IEnumerable<EntityAttributeConfigurationCreateUpdateRequest> newAttributes =
            attributesRequest.Where(x => x is AttributeConfigurationCreateUpdateRequest);

        foreach (EntityAttributeConfigurationCreateUpdateRequest attribute in newAttributes)
        {
            machineNames.Add(
                ((AttributeConfigurationCreateUpdateRequest)attribute).MachineName!
            );
        }

        if (machineNames.GroupBy(x => x).Any(x => x.Count() > 1))
        {
            return false;
        }

        return true;
    }

    private void InitializeAttributeInstanceWithExternalValuesFromEntity(
        EntityConfiguration entityConfiguration,
        AttributeConfiguration attributeConfiguration,
        AttributeInstance? attributeInstance
    )
    {
        switch (attributeConfiguration.ValueType)
        {
            case EavAttributeType.Serial:
                {
                    if (attributeInstance == null)
                    {
                        return;
                    }

                    var serialAttributeConfiguration = attributeConfiguration as SerialAttributeConfiguration;

                    var serialInstance = attributeInstance as SerialAttributeInstance;

                    if (serialAttributeConfiguration == null || serialInstance == null)
                    {
                        throw new ArgumentException("Invalid attribute type");
                    }

                    EntityConfigurationAttributeReference? entityAttribute = entityConfiguration.Attributes
                        .FirstOrDefault(x => x.AttributeConfigurationId == attributeConfiguration.Id);

                    if (entityAttribute == null)
                    {
                        throw new NotFoundException("Attribute not found");
                    }

                    var existingAttributeValue =
                        entityAttribute.AttributeConfigurationExternalValues.FirstOrDefault();

                    long? deserializedValue = null;

                    if (existingAttributeValue != null)
                    {
                        deserializedValue = JsonSerializer.Deserialize<long>(existingAttributeValue.ToString()!);
                    }

                    var newExternalValue = existingAttributeValue == null
                        ? serialAttributeConfiguration.StartingNumber
                        : deserializedValue += serialAttributeConfiguration.Increment;

                    serialInstance.Value = newExternalValue!.Value;

                    entityConfiguration.UpdateAttrributeExternalValues(attributeConfiguration.Id,
                        new List<object> { newExternalValue }
                    );
                }
                break;
        }
    }

    /// <summary>
    /// Update entity configuration external values.
    /// </summary>
    /// <remarks>
    /// Specialized method to update entity configuration external value with updating arrtibute instance value -
    /// this means new instance value is not out of external value logic, and can be overwritten to it.
    /// Intended use: update with a new value an attribute instance
    /// whose value was initialized from the external entity configuration values.
    ///
    /// Note that after exetuting this method entity configuration aggregate repository has uncommited events,
    /// use .SaveAsync() for saving.
    /// </remarks>
    /// <param name="entityConfiguration"></param>
    /// <param name="attributeConfiguration"></param>
    /// <param name="attributeInstance"></param>
    /// <returns>
    /// List of validation errors or null if everithing is fine.
    /// </returns>
    private List<string>? UpdateEntityExternalValuesDuringInstanceUpdate(
        EntityConfiguration entityConfiguration,
        AttributeConfiguration attributeConfiguration,
        AttributeInstance? attributeInstance
    )
    {
        switch (attributeConfiguration.ValueType)
        {
            case EavAttributeType.Serial:
                {
                    if (attributeInstance == null)
                    {
                        return null;
                    }

                    var validationErrors = new List<string>();

                    var serialAttributeConfiguration = attributeConfiguration as SerialAttributeConfiguration;

                    var serialInstance = attributeInstance as SerialAttributeInstance;

                    if (serialAttributeConfiguration == null || serialInstance == null)
                    {
                        validationErrors.Add("Invalid attribute type.");
                    }

                    if (serialInstance != null && !serialInstance.Value.HasValue)
                    {
                        validationErrors.Add("Updating serial number value can not be empty.");
                    }

                    EntityConfigurationAttributeReference? entityAttribute = entityConfiguration.Attributes
                        .FirstOrDefault(x => x.AttributeConfigurationId == attributeConfiguration.Id);

                    if (entityAttribute == null)
                    {
                        validationErrors.Add("Attribute configuration is not found.");
                    }

                    if (validationErrors.Count > 0)
                    {
                        return validationErrors;
                    }

                    var existingAttributeValue =
                        entityAttribute!.AttributeConfigurationExternalValues.First();

                    long? deserializedValue = JsonSerializer.Deserialize<long>(existingAttributeValue!.ToString()!);

                    if (serialInstance!.Value <= deserializedValue!.Value)
                    {
                        validationErrors.Add("Serial number value can not be less than the already existing one.");
                        return validationErrors;
                    }

                    var newExternalValue = serialInstance.Value;

                    entityConfiguration.UpdateAttrributeExternalValues(attributeConfiguration.Id,
                        new List<object> { newExternalValue! }
                    );

                    return null;
                }
        }
        return null;
    }

    private async Task<ProjectionQueryResult<AttributeConfigurationListItemViewModel>> GetAttributesByIds(
        List<Guid> attributesIds, CancellationToken cancellationToken)
    {
        // create attributes filter
        Filter attributeIdFilter = new(nameof(AttributeConfigurationProjectionDocument.Id),
            FilterOperator.Equal,
            attributesIds[0]
        );

        foreach (Guid attributesId in attributesIds.Skip(1))
        {
            attributeIdFilter.Filters.Add(
                new FilterConnector(
                    FilterLogic.Or,
                    new Filter(nameof(AttributeConfigurationProjectionDocument.Id),
                        FilterOperator.Equal,
                        attributesId
                    )
                )
            );
        }

        ProjectionQueryResult<AttributeConfigurationListItemViewModel> attributes = await ListAttributes(
            new ProjectionQuery { Filters = new List<Filter> { attributeIdFilter } },
            cancellationToken: cancellationToken
        );

        return attributes;
    }

    private async Task<(string?, ProblemDetails?)> BuildCategoryPath(Guid treeId, Guid? parentId,
        CancellationToken cancellationToken)
    {
        CategoryTree? tree = await _categoryTreeRepository.LoadAsync(treeId, treeId.ToString(), cancellationToken)
            .ConfigureAwait(false);
        if (tree == null)
        {
            return (null, new ValidationErrorResponse("TreeId", "Tree not found"))!;
        }

        Category? parent = parentId == null
            ? null
            : _mapper.Map<Category>(await _entityInstanceRepository
                .LoadAsync(parentId.Value, tree.EntityConfigurationId.ToString(), cancellationToken)
                .ConfigureAwait(false)
            );

        if (parent == null && parentId != null)
        {
            return (null, new ValidationErrorResponse("ParentId", "Parent category not found"))!;
        }

        CategoryPath? parentPath = parent?.CategoryPaths.FirstOrDefault(x => x.TreeId == treeId);
        var categoryPath = parentPath == null ? "" : $"{parentPath.Path}/{parent?.Id}";
        return (categoryPath, null)!;
    }

    #region EntityConfiguration

    public async Task<EntityConfigurationViewModel> GetEntityConfiguration(Guid id)
    {
        EntityConfiguration? entityConfiguration = await _entityConfigurationRepository.LoadAsync(id, id.ToString());

        return _mapper.Map<EntityConfigurationViewModel>(entityConfiguration);
    }

    public async Task<EntityConfigurationWithAttributesViewModel> GetEntityConfigurationWithAttributes(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var entityConfiguration = await _entityConfigurationRepository.LoadAsyncOrThrowNotFound(
            id,
            id.ToString(), // EntityConfiguration partition key is set to it's id in Domain model
            cancellationToken
        );

        var entityConfigurationViewModel = _mapper.Map<EntityConfigurationWithAttributesViewModel>(entityConfiguration);

        var attributes = await GetAttributeConfigurationsForEntityConfiguration(
            entityConfiguration,
            cancellationToken
        );

        entityConfigurationViewModel.Attributes = _mapper.Map<List<AttributeConfigurationViewModel>>(attributes);

        return entityConfigurationViewModel;
    }

    public async Task<ProjectionQueryResult<AttributeConfigurationListItemViewModel>> ListAttributes(
        ProjectionQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ProjectionQueryResult<AttributeConfigurationProjectionDocument> list =
            await _attributeConfigurationProjectionRepository.Query(query, null, cancellationToken);
        return _mapper.Map<ProjectionQueryResult<AttributeConfigurationListItemViewModel>>(list);
    }

    public async Task<ProjectionQueryResult<EntityConfigurationViewModel>> ListEntityConfigurations(
        ProjectionQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ProjectionQueryResult<EntityConfigurationProjectionDocument> records =
            await _entityConfigurationProjectionRepository.Query(query, null, cancellationToken);
        return _mapper.Map<ProjectionQueryResult<EntityConfigurationViewModel>>(records);
    }

    public async Task<(AttributeConfigurationViewModel?, ValidationErrorResponse?)> CreateAttribute(
        AttributeConfigurationCreateUpdateRequest attributeConfigurationCreateUpdateRequest,
        CancellationToken cancellationToken = default
    )
    {
        EnsureAttributeMachineNameIsAdded(attributeConfigurationCreateUpdateRequest);

        var attribute = _mapper.Map<AttributeConfiguration>(attributeConfigurationCreateUpdateRequest);
        List<string> validationErrors = attribute.Validate();
        if (validationErrors.Any())
        {
            return (null, new ValidationErrorResponse(attribute.MachineName, validationErrors.ToArray()));
        }

        // make sure that a projection exists for attribute list because once we save the attribute
        // a projection builder will be triggered to create a projection of this attribute.
        await _attributeConfigurationProjectionRepository.EnsureIndex(cancellationToken).ConfigureAwait(false);

        // Create and add array element configuration
        if (attributeConfigurationCreateUpdateRequest is ArrayAttributeConfigurationCreateUpdateRequest array)
        {
            var arrayItemConfigurationId =
                await CreateArrayElementConfiguration(array.ItemsType,
                    array.MachineName ?? "array",
                    array.ItemsAttributeConfiguration,
                    array.TenantId,
                    cancellationToken);
            if (arrayItemConfigurationId != null)
            {
                ((attribute as ArrayAttributeConfiguration)!).UpdateItemsAttributeConfigurationId(
                    arrayItemConfigurationId.Value
                );
            }

        }

        await _attributeConfigurationRepository.SaveAsync(_userInfo, attribute, cancellationToken)
            .ConfigureAwait(false);

        return (_mapper.Map<AttributeConfigurationViewModel>(attribute), null);
    }



    public async Task<AttributeConfigurationViewModel> GetAttribute(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        AttributeConfiguration attribute = await _attributeConfigurationRepository
            .LoadAsyncOrThrowNotFound(id, id.ToString(), cancellationToken);

        if (attribute.IsDeleted)
        {
            throw new NotFoundException("Attribute not found");
        }

        return _mapper.Map<AttributeConfigurationViewModel>(attribute);
    }

    public async Task<(AttributeConfigurationViewModel?, ProblemDetails?)> UpdateAttribute(
        Guid id, AttributeConfigurationCreateUpdateRequest updateRequest,
        CancellationToken cancellationToken = default)
    {
        AttributeConfiguration? attribute = await _attributeConfigurationRepository
            .LoadAsync(id, id.ToString(), cancellationToken);

        if (attribute == null || attribute.IsDeleted)
        {
            return (null, new ValidationErrorResponse(nameof(id), "Attribute not found"));
        }

        if (attribute.ValueType != updateRequest.ValueType)
        {
            return (null,
                new ValidationErrorResponse(nameof(updateRequest.ValueType), "Attribute type cannot be changed"));
        }

        updateRequest.MachineName = attribute.MachineName;
        var updatedAttribute = _mapper.Map<AttributeConfiguration>(updateRequest);

        attribute.UpdateAttribute(updatedAttribute);

        List<string> validationErrors = attribute.Validate();
        if (validationErrors.Any())
        {
            return (null, new ValidationErrorResponse(updatedAttribute.MachineName, validationErrors.ToArray()));
        }

        await _attributeConfigurationProjectionRepository.EnsureIndex(cancellationToken).ConfigureAwait(false);

        await _attributeConfigurationRepository.SaveAsync(_userInfo, attribute, cancellationToken)
            .ConfigureAwait(false);

        return (_mapper.Map<AttributeConfigurationViewModel>(attribute), null);
    }

    public async Task<(EntityConfigurationViewModel?, ProblemDetails?)> CreateEntityConfiguration(
        EntityConfigurationCreateRequest createRequest,
        CancellationToken cancellationToken
    )
    {
        // We modify request attributes below to replace them with id references to attributes in the store
        // It's not a good practice to modify something which comes from api consumer, so we make a copy of
        // the request with automapper.
        var entityConfigurationCreateRequest = new EntityConfigurationCreateRequest();
        _mapper.Map(createRequest, entityConfigurationCreateRequest);

        foreach (EntityAttributeConfigurationCreateUpdateRequest attribute in entityConfigurationCreateRequest
                     .Attributes
                     .Where(x => x is AttributeConfigurationCreateUpdateRequest))
        {
            EnsureAttributeMachineNameIsAdded(
                (AttributeConfigurationCreateUpdateRequest)attribute
            );
        }

        if (!await CheckAttributesListMachineNameUnique(entityConfigurationCreateRequest.Attributes, cancellationToken)
                .ConfigureAwait(false))
        {
            return (
                null,
                new ValidationErrorResponse(
                    nameof(entityConfigurationCreateRequest.Attributes),
                    "Attributes machine name must be unique"
                )
            )!;
        }

        foreach (EntityAttributeConfigurationCreateUpdateRequest attribute in entityConfigurationCreateRequest
                     .Attributes
                     .Where(x => x is EntityAttributeConfigurationCreateUpdateReferenceRequest))
        {
            var requestAttribute = (EntityAttributeConfigurationCreateUpdateReferenceRequest)attribute;

            AttributeConfiguration attributeConfiguration = await _attributeConfigurationRepository
                .LoadAsyncOrThrowNotFound(
                    requestAttribute.AttributeConfigurationId,
                    requestAttribute.AttributeConfigurationId.ToString(),
                    cancellationToken
                ).ConfigureAwait(false);

            if (attributeConfiguration.IsDeleted)
            {
                return (
                    null,
                    new ValidationErrorResponse(
                        nameof(entityConfigurationCreateRequest.Attributes),
                        "One or more attribute not found"
                    )
                )!;
            }
        }

        var allAttrProblemDetails = new List<ValidationErrorResponse>();
        for (var i = 0; i < entityConfigurationCreateRequest.Attributes.Count; i++)
        {
            EntityAttributeConfigurationCreateUpdateRequest attribute =
                entityConfigurationCreateRequest.Attributes[i];

            if (
            !(attribute is EntityAttributeConfigurationCreateUpdateReferenceRequest))
            {
                (AttributeConfigurationViewModel? attrCreated, ValidationErrorResponse? attrProblemDetails) =
                    await CreateAttribute(
                        (AttributeConfigurationCreateUpdateRequest)attribute,
                        cancellationToken
                    ).ConfigureAwait(false);

                if (attrProblemDetails != null)
                {
                    allAttrProblemDetails.Add(attrProblemDetails);
                }
                else
                {
                    entityConfigurationCreateRequest.Attributes[i] =
                        new EntityAttributeConfigurationCreateUpdateReferenceRequest
                        {
                            AttributeConfigurationId = attrCreated.Id
                        };
                }
            }
        }

        if (allAttrProblemDetails.Any())
        {
            Dictionary<string, string[]> allErrors = allAttrProblemDetails.SelectMany(pd => pd.Errors)
                .ToLookup(pair => pair.Key, pair => pair.Value)
                .ToDictionary(group => group.Key, group => group.First());
            return (
                null,
                new ValidationErrorResponse(allErrors)
            );
        }

        var entityConfiguration = new EntityConfiguration(
            Guid.NewGuid(),
            _mapper.Map<List<LocalizedString>>(entityConfigurationCreateRequest.Name),
            entityConfigurationCreateRequest.MachineName,
            _mapper.Map<List<EntityConfigurationAttributeReference>>(entityConfigurationCreateRequest.Attributes),
            entityConfigurationCreateRequest.TenantId
        );

        List<string> entityValidationErrors = entityConfiguration.Validate();
        if (entityValidationErrors.Any())
        {
            return (null,
                new ValidationErrorResponse(entityConfiguration.MachineName, entityValidationErrors.ToArray()));
        }

        await _entityConfigurationProjectionRepository.EnsureIndex(cancellationToken).ConfigureAwait(false);

        await _entityConfigurationRepository.SaveAsync(
            _userInfo,
            entityConfiguration,
            cancellationToken
        ).ConfigureAwait(false);

        return (_mapper.Map<EntityConfigurationViewModel>(entityConfiguration), null)!;
    }

    public async Task<(EntityConfigurationViewModel?, ProblemDetails?)> UpdateEntityConfiguration(
        EntityConfigurationUpdateRequest entityUpdateRequest,
        CancellationToken cancellationToken)
    {
        foreach (EntityAttributeConfigurationCreateUpdateRequest attribute in entityUpdateRequest.Attributes.Where(
                     x => x is AttributeConfigurationCreateUpdateRequest
                 ))
        {
            EnsureAttributeMachineNameIsAdded(
                (AttributeConfigurationCreateUpdateRequest)attribute
            );
        }

        if (!await CheckAttributesListMachineNameUnique(entityUpdateRequest.Attributes, cancellationToken)
                .ConfigureAwait(false))
        {
            return (
                null,
                new ValidationErrorResponse(nameof(entityUpdateRequest.Attributes),
                    "Attributes machine name must be unique"
                )
            )!;
        }

        EntityConfiguration? entityConfiguration = await _entityConfigurationRepository.LoadAsync(
            entityUpdateRequest.Id,
            entityUpdateRequest.Id.ToString(),
            cancellationToken
        ).ConfigureAwait(false);

        if (entityConfiguration == null)
        {
            return (null,
                new ValidationErrorResponse(nameof(entityUpdateRequest.Id), "Entity configuration not found"))!;
        }

        var entityConfigurationExistingAttributes =
            await GetAttributeConfigurationsForEntityConfiguration(entityConfiguration, cancellationToken);

        // Update config name
        foreach (LocalizedStringCreateRequest name in entityUpdateRequest.Name.Where(name =>
                     !entityConfiguration.Name.Any(
                         x => x.CultureInfoId == name.CultureInfoId && x.String == name.String
                     )
                 ))
        {
            entityConfiguration.UpdateName(name.String, name.CultureInfoId);
        }

        var reservedAttributes = new List<Guid>();
        var allAttrProblemDetails = new List<ValidationErrorResponse>();

        foreach (EntityAttributeConfigurationCreateUpdateRequest attributeUpdate in entityUpdateRequest.Attributes)
        {
            if (attributeUpdate is EntityAttributeConfigurationCreateUpdateReferenceRequest
                attributeReferenceUpdate)
            {
                // for references we need to just add/remove the reference
                var attributeShouldBeAdded = entityConfiguration.Attributes
                    .All(a => a.AttributeConfigurationId != attributeReferenceUpdate.AttributeConfigurationId);

                AttributeConfiguration? attributeConfiguration = await _attributeConfigurationRepository.LoadAsync(
                    attributeReferenceUpdate.AttributeConfigurationId,
                    attributeReferenceUpdate.AttributeConfigurationId.ToString(),
                    cancellationToken
                ).ConfigureAwait(false);

                if (attributeConfiguration != null && !attributeConfiguration.IsDeleted)
                {
                    if (attributeShouldBeAdded)
                    {
                        entityConfiguration.AddAttribute(attributeConfiguration.Id);
                    }

                    reservedAttributes.Add(attributeConfiguration.Id);
                }
            }
            else if (attributeUpdate is AttributeConfigurationCreateUpdateRequest attributeCreateRequest)
            {
                // Make sure such attribute does not already exist on the entity
                if (entityConfigurationExistingAttributes.Any(c => c.MachineName == attributeCreateRequest.MachineName))
                {
                    return (null,
                        new ValidationErrorResponse(
                            $"Attributes[{entityUpdateRequest.Attributes.IndexOf(attributeUpdate)}]",
                            "UpdateEntityConfiguration method should not be used to update individual attribute configurations. " +
                            "Attribute configurations are separate entities and can be attached to multiple entity configurations. " +
                            "That means that updating entity configuration only means updating it's name and ONLY adding/removing attributes." +
                            "Although it's possible to create new attributes by simply adding full attribute request to update method - that will just create a new attribute and add reference to this entity configuration."
                        )
                    );
                }
                else
                {
                    (AttributeConfigurationViewModel? attributeCreated, ValidationErrorResponse? attrProblemDetails) =
                        await CreateAttribute(
                            attributeCreateRequest,
                            cancellationToken
                        ).ConfigureAwait(false);

                    if (attrProblemDetails != null)
                    {
                        allAttrProblemDetails.Add(attrProblemDetails);
                    }
                    else
                    {
                        entityConfiguration.AddAttribute(attributeCreated.Id);
                        reservedAttributes.Add(attributeCreated.Id);
                    }
                }
            }
        }

        if (allAttrProblemDetails.Any())
        {
            Dictionary<string, string[]> allErrors = allAttrProblemDetails.SelectMany(pd => pd.Errors)
                .ToLookup(pair => pair.Key, pair => pair.Value)
                .ToDictionary(group => group.Key, group => group.First());
            return (
                null,
                new ValidationErrorResponse(allErrors)
            );
        }

        IEnumerable<EntityConfigurationAttributeReference> attributesToRemove =
            entityConfiguration.Attributes.ExceptBy(
                reservedAttributes,
                x => x.AttributeConfigurationId
            );

        foreach (EntityConfigurationAttributeReference attribute in attributesToRemove)
        {
            entityConfiguration.RemoveAttribute(attribute.AttributeConfigurationId);
        }

        List<string> entityValidationErrors = entityConfiguration.Validate();
        if (entityValidationErrors.Any())
        {
            return (null,
                new ValidationErrorResponse(entityConfiguration.MachineName, entityValidationErrors.ToArray()));
        }

        await _entityConfigurationProjectionRepository.EnsureIndex(cancellationToken).ConfigureAwait(false);
        await _entityConfigurationRepository.SaveAsync(_userInfo, entityConfiguration, cancellationToken)
            .ConfigureAwait(false);

        return (_mapper.Map<EntityConfigurationViewModel>(entityConfiguration), null)!;
    }

    public async Task<(EntityConfigurationViewModel?, ProblemDetails?)> AddAttributeToEntityConfiguration(
        Guid attributeId,
        Guid entityConfigurationId,
        CancellationToken cancellationToken = default
    )
    {
        AttributeConfiguration attributeConfiguration = await _attributeConfigurationRepository
            .LoadAsyncOrThrowNotFound(
                attributeId,
                attributeId.ToString(),
                cancellationToken
            ).ConfigureAwait(false);

        if (attributeConfiguration.IsDeleted)
        {
            return (null, new ValidationErrorResponse(nameof(attributeId), "Attribute not found"));
        }

        EntityConfiguration entityConfiguration = await _entityConfigurationRepository.LoadAsyncOrThrowNotFound(
            entityConfigurationId,
            entityConfigurationId.ToString(),
            cancellationToken
        ).ConfigureAwait(false);

        if (entityConfiguration.Attributes.Any(x => x.AttributeConfigurationId == attributeConfiguration.Id))
        {
            return (null, new ValidationErrorResponse(nameof(attributeId), "Attribute has already been added"))!;
        }

        if (!await IsAttributeMachineNameUniqueForEntityConfiguration(
                attributeConfiguration.MachineName,
                entityConfiguration,
                cancellationToken
            ).ConfigureAwait(false)
           )
        {
            return (null,
                new ValidationErrorResponse(nameof(attributeId), "Attributes machine name must be unique"));
        }

        entityConfiguration.AddAttribute(attributeId);
        await _entityConfigurationProjectionRepository.EnsureIndex(cancellationToken).ConfigureAwait(false);

        await _entityConfigurationRepository.SaveAsync(_userInfo, entityConfiguration, cancellationToken)
            .ConfigureAwait(false);

        return (_mapper.Map<EntityConfigurationViewModel>(entityConfiguration), null)!;
    }

    public async Task<(AttributeConfigurationViewModel, ProblemDetails)> CreateAttributeAndAddToEntityConfiguration(
        AttributeConfigurationCreateUpdateRequest attributeConfigurationCreateUpdateRequest,
        Guid entityConfigurationId,
        CancellationToken cancellationToken = default
    )
    {
        EnsureAttributeMachineNameIsAdded(attributeConfigurationCreateUpdateRequest);

        EntityConfiguration entityConfiguration = await _entityConfigurationRepository.LoadAsyncOrThrowNotFound(
            entityConfigurationId,
            entityConfigurationId.ToString(),
            cancellationToken
        ).ConfigureAwait(false);

        if (!await IsAttributeMachineNameUniqueForEntityConfiguration(
                attributeConfigurationCreateUpdateRequest.MachineName,
                entityConfiguration,
                cancellationToken
            )
           )
        {
            return (
                null,
                new ValidationErrorResponse(nameof(attributeConfigurationCreateUpdateRequest.MachineName),
                    "Machine name already exists in this configuration. Please consider using different name"
                )
            )!;
        }

        (AttributeConfigurationViewModel? createdAttribute, ValidationErrorResponse? attrProblemDetails) =
            await CreateAttribute(attributeConfigurationCreateUpdateRequest, cancellationToken);

        if (attrProblemDetails != null)
        {
            return (null, attrProblemDetails);
        }

        entityConfiguration.AddAttribute(createdAttribute.Id);
        await _attributeConfigurationProjectionRepository.EnsureIndex(cancellationToken).ConfigureAwait(false);

        await _entityConfigurationRepository.SaveAsync(_userInfo, entityConfiguration, cancellationToken);

        return (createdAttribute, null)!;
    }

    public async Task DeleteAttributesFromEntityConfiguration(List<Guid> attributesIds, Guid entityConfigurationId,
        CancellationToken cancellationToken = default)
    {
        EntityConfiguration entityConfiguration = await _entityConfigurationRepository.LoadAsyncOrThrowNotFound(
            entityConfigurationId,
            entityConfigurationId.ToString(),
            cancellationToken
        );

        List<AttributeConfiguration> listAttributesConfigurations =
            await GetAttributeConfigurationsForEntityConfiguration(entityConfiguration, cancellationToken);

        foreach (Guid attributeId in attributesIds)
        {
            if (listAttributesConfigurations.Any(x => x.Id == attributeId))
            {
                entityConfiguration.RemoveAttribute(attributeId);
            }
        }

        await _entityConfigurationProjectionRepository.EnsureIndex(cancellationToken).ConfigureAwait(false);

        await _entityConfigurationRepository.SaveAsync(_userInfo, entityConfiguration, cancellationToken);
    }

    public async Task DeleteAttributes(List<Guid> attributesIds, CancellationToken cancellationToken = default)
    {
        foreach (Guid attributeId in attributesIds)
        {
            AttributeConfiguration? attributeConfiguration = await _attributeConfigurationRepository.LoadAsync(
                attributeId,
                attributeId.ToString(),
                cancellationToken
            );

            if (attributeConfiguration != null && !attributeConfiguration.IsDeleted)
            {
                attributeConfiguration.Delete();

                await _attributeConfigurationRepository
                    .SaveAsync(_userInfo, attributeConfiguration, cancellationToken).ConfigureAwait(false);
            }
        }

        var filterPropertyName = string.Concat(
            nameof(EntityConfigurationProjectionDocument.Attributes),
            ".",
            nameof(AttributeConfigurationReference.AttributeConfigurationId
            )
        );

        Filter filters = new(
            filterPropertyName,
            FilterOperator.Equal,
            attributesIds[0]
        );

        foreach (Guid attributeId in attributesIds.Skip(1))
        {
            filters.Filters.Add(new FilterConnector(
                    FilterLogic.Or,
                    new Filter(
                        filterPropertyName,
                        FilterOperator.Equal,
                        attributeId
                    )
                )
            );
        }

        ProjectionQueryResult<EntityConfigurationProjectionDocument> entityConfigurations =
            await _entityConfigurationProjectionRepository.Query(
                new ProjectionQuery { Filters = new List<Filter> { filters } },
                cancellationToken: cancellationToken
            );

        if (entityConfigurations.Records.Count > 0)
        {
            IEnumerable<Guid?> entitiesIdsFromQuery = entityConfigurations.Records.Select(x => x.Document.Id);

            foreach (Guid? entityId in entitiesIdsFromQuery)
            {
                try
                {
                    await DeleteAttributesFromEntityConfiguration(attributesIds, entityId.Value, cancellationToken);
                }
                catch (NotFoundException)
                {
                    _logger.LogWarning(@"Entity {EntityId} not found", entityId);
                }
            }
        }
    }
private async Task<Guid?> CreateArrayElementConfiguration(EavAttributeType type, string machineName, AttributeConfigurationCreateUpdateRequest? attributeConfigurationCreateUpdateRequest, Guid? tenantId, CancellationToken cancellationToken)
    {
        Guid? resultGuid = null;

        if (attributeConfigurationCreateUpdateRequest == null)
        {
            var defaultTypeConfig = DefaultAttributeConfigurationFactory.GetDefaultConfiguration(type, $"element_config_{machineName}", tenantId);
            if (defaultTypeConfig != null)
            {
                var saved = await _attributeConfigurationRepository.SaveAsync(_userInfo, defaultTypeConfig, cancellationToken).ConfigureAwait(false);
                resultGuid = saved ? defaultTypeConfig.Id : null;
            }
        }
        else
        {
            FillMissedValuesInConfiguration(attributeConfigurationCreateUpdateRequest, machineName, tenantId);
            (AttributeConfigurationViewModel? childItemConfig, ValidationErrorResponse? arrayItemsConfigurationErrors) = await
                CreateAttribute(attributeConfigurationCreateUpdateRequest, cancellationToken);

            if (arrayItemsConfigurationErrors != null || childItemConfig == null)
            {
                return null;
            }
            resultGuid = childItemConfig.Id;
        }
        return resultGuid;
    }

    private void FillMissedValuesInConfiguration(AttributeConfigurationCreateUpdateRequest attributeConfigurationCreateUpdateRequest, string machineName, Guid? tenantId)
    {
        attributeConfigurationCreateUpdateRequest.TenantId = tenantId;

        if (attributeConfigurationCreateUpdateRequest.Name == null
            || attributeConfigurationCreateUpdateRequest.Name.Count < 1)
        {
            attributeConfigurationCreateUpdateRequest.Name = new List<LocalizedStringCreateRequest>
            {
                new LocalizedStringCreateRequest()
                {
                    CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID,
                    String = $"Items configuration of {machineName}"
                }
            };

        }
        if (attributeConfigurationCreateUpdateRequest.Description == null ||
            attributeConfigurationCreateUpdateRequest.Description.Count < 1)
        {
            attributeConfigurationCreateUpdateRequest.Description = new List<LocalizedStringCreateRequest>()
            {
                new LocalizedStringCreateRequest()
                {
                    CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID,
                    String = $"This attribute was created for {machineName} attribute. " +
                             $"Since that attribute is an array and has it's own configuration, a separate attribute " +
                             $"is required to configure array elements."
                }
            };
        }
    }
    #endregion

    // public async Task<List<EntityInstanceViewModel>> ListEntityInstances(string entityConfigurationMachineName, int take, int skip = 0)
    // {
    //     var records = await _entityInstanceRepository
    //         .GetQuery()
    //         .Where(e => e.EntityConfiguration.MachineName == entityConfigurationMachineName)
    //         .Take(take)
    //         .Skip(skip)
    //         .ToListAsync();
    //
    //     return _mapper.Map<List<EntityInstanceViewModel>>(records);
    // }
    //
    // public async Task<List<EntityInstanceViewModel>> ListEntityInstances(Guid entityConfigurationId, int take, int skip = 0)
    // {
    //     var records = await _entityInstanceRepository
    //         .GetQuery()
    //         .Where(e => e.EntityConfigurationId == entityConfigurationId)
    //         .Take(take)
    //         .Skip(skip)
    //         .ToListAsync();
    //
    //     return _mapper.Map<List<EntityInstanceViewModel>>(records);
    // }

    #region Categories

    public async Task<(HierarchyViewModel, ProblemDetails)> CreateCategoryTreeAsync(
        CategoryTreeCreateRequest entity,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    )
    {
        EntityConfiguration? entityConfiguration = await _entityConfigurationRepository.LoadAsync(
            entity.EntityConfigurationId,
            entity.EntityConfigurationId.ToString(),
            cancellationToken
        ).ConfigureAwait(false);

        if (entityConfiguration == null)
        {
            return (null, new ValidationErrorResponse("EntityConfigurationId", "Configuration not found"))!;
        }

        var tree = new CategoryTree(
            Guid.NewGuid(),
            entity.EntityConfigurationId,
            entity.MachineName,
            tenantId
        );

        _ = await _categoryTreeRepository.SaveAsync(_userInfo, tree, cancellationToken).ConfigureAwait(false);
        return (_mapper.Map<HierarchyViewModel>(tree), null)!;
    }

    /// <summary>
    /// Create new category from provided json string.
    /// </summary>
    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "name": "Main Category",
    ///     "desprition": "Main Category description",
    ///     "entityConfigurationId": "fb80cb74-6f47-4d38-bb87-25bd820efee7",
    ///     "categoryTreeId": "65053391-9f0e-4b86-959e-2fe342e705d4",
    ///     "parentId": "3e302832-ce6b-4c41-9cf8-e2b3fdd7b01c",
    ///     "tenantId": "b6842a71-162b-411d-86e9-3ec01f909c82"
    /// }
    /// ```
    ///
    /// Where "name" and "description" are attributes machine names,
    /// "entityConfigurationId" - obviously the id of entity configuration which has all category attributes,
    /// "categoryTreeId" - guid of category tree, which represents separated hirerarchy with relations between categories
    /// "parentId" - id guid of category from which new branch of hierarchy will be built.
    /// Can be null if placed at the root of category tree.
    /// "tenantId" - tenant id guid. A guid which uniquely identifies and isolates the data. For single tenant
    /// application this should be one hardcoded guid for whole app.
    ///
    /// </remarks>
    /// <param name="categoryJsonString"></param>
    /// <param name="requestDeserializedCallback">
    /// <![CDATA[ Task<CategoryInstanceCreateRequest>(CategoryInstanceCreateRequest createRequest); ]]>
    ///
    /// This function will be called after deserializing the request from json
    /// to CategoryInstanceCreateRequest and allows adding additional validation or any other pre-processing logic.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<(JsonDocument?, ProblemDetails?)> CreateCategoryInstance(
        string categoryJsonString,
        Func<CategoryInstanceCreateRequest, Task<CategoryInstanceCreateRequest>>? requestDeserializedCallback = null,
        CancellationToken cancellationToken = default
    )
    {
        JsonDocument categoryJson = JsonDocument.Parse(categoryJsonString);

        return CreateCategoryInstance(
            categoryJson.RootElement,
            requestDeserializedCallback,
            cancellationToken
        );
    }

    /// <summary>
    /// Create new category from provided json string.
    /// </summary>
    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "name": "Main Category",
    ///     "desprition": "Main Category description"
    /// }
    /// ```
    ///
    /// Where "name" and "description" are attributes machine names.
    /// Note that this overload accepts "entityConfigurationId", "categoryTreeId", "parentId" and "tenantId" via method arguments,
    /// so they should not be in json.
    ///
    /// </remarks>
    /// <param name="categoryJsonString"></param>
    /// <param name="categoryConfigurationId">id of entity configuration which has all category attributes</param>
    /// <param name="categoryTreeId">id of category tree, which represents separated hirerarchy with relations between categories</param>
    /// <param name="parentId">id of category from which new branch of hierarchy will be built. Can be null if placed at the root of category tree.</param>
    /// <param name="tenantId">tenant id guid. A guid which uniquely identifies and isolates the data. For single
    /// tenant application this should be one hardcoded guid for whole app.</param>
    /// <param name="requestDeserializedCallback">
    /// <![CDATA[ Task<CategoryInstanceCreateRequest>(CategoryInstanceCreateRequest createRequest); ]]>
    ///
    /// This function will be called after deserializing the request from json
    /// to CategoryInstanceCreateRequest and allows adding additional validation or any other pre-processing logic.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<(JsonDocument?, ProblemDetails?)> CreateCategoryInstance(
        string categoryJsonString,
        Guid categoryConfigurationId,
        Guid categoryTreeId,
        Guid? parentId,
        Guid? tenantId,
        Func<CategoryInstanceCreateRequest, Task<CategoryInstanceCreateRequest>>? requestDeserializedCallback = null,
        CancellationToken cancellationToken = default
    )
    {
        JsonDocument categoryJson = JsonDocument.Parse(categoryJsonString);

        return CreateCategoryInstance(
            categoryJson.RootElement,
            categoryConfigurationId,
            categoryTreeId,
            parentId,
            tenantId,
            requestDeserializedCallback,
            cancellationToken
        );
    }

    /// <summary>
    /// Create new category from provided json document.
    /// </summary>
    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "name": "Main Category",
    ///     "desprition": "Main Category description",
    ///     "entityConfigurationId": "fb80cb74-6f47-4d38-bb87-25bd820efee7",
    ///     "categoryTreeId": "65053391-9f0e-4b86-959e-2fe342e705d4",
    ///     "parentId": "3e302832-ce6b-4c41-9cf8-e2b3fdd7b01c",
    ///     "tenantId": "b6842a71-162b-411d-86e9-3ec01f909c82"
    /// }
    /// ```
    ///
    /// Where "name" and "description" are attributes machine names,
    /// "entityConfigurationId" - obviously the id of entity configuration which has all category attributes,
    /// "categoryTreeId" - guid of category tree, which represents separated hirerarchy with relations between categories
    /// "parentId" - id guid of category from which new branch of hierarchy will be built.
    /// Can be null if placed at the root of category tree.
    /// "tenantId" - tenant id guid. A guid which uniquely identifies and isolates the data. For single tenant
    /// application this should be one hardcoded guid for whole app.
    ///
    /// </remarks>
    /// <param name="categoryJson"></param>
    /// <param name="requestDeserializedCallback">
    /// <![CDATA[ Task<CategoryInstanceCreateRequest>(CategoryInstanceCreateRequest createRequest); ]]>
    ///
    /// This function will be called after deserializing the request from json
    /// to CategoryInstanceCreateRequest and allows adding additional validation or any other pre-processing logic.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(JsonDocument?, ProblemDetails?)> CreateCategoryInstance(
        JsonElement categoryJson,
        Func<CategoryInstanceCreateRequest, Task<CategoryInstanceCreateRequest>>? requestDeserializedCallback = null,
        CancellationToken cancellationToken = default
    )
    {
        var (categoryInstanceCreateRequest, deserializationErrors) =
           await DeserializeCategoryInstanceCreateRequestFromJson(categoryJson, cancellationToken);

        if (deserializationErrors != null)
        {
            return (null, deserializationErrors);
        }

        return await CreateCategoryInstance(
            categoryJson,
            categoryInstanceCreateRequest!.CategoryConfigurationId,
            categoryInstanceCreateRequest.CategoryTreeId,
            categoryInstanceCreateRequest.ParentId,
            categoryInstanceCreateRequest.TenantId,
            requestDeserializedCallback,
            cancellationToken
        );
    }

    /// <summary>
    /// Create new category from provided json document.
    /// </summary>
    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "name": "Main Category",
    ///     "desprition": "Main Category description"
    /// }
    /// ```
    ///
    /// Where "name" and "description" are attributes machine names.
    /// Note that this overload accepts "entityConfigurationId", "categoryTreeId", "parentId" and "tenantId" via method arguments,
    /// so they should not be in json.
    ///
    /// </remarks>
    /// <param name="categoryJson"></param>
    /// <param name="categoryConfigurationId">id of entity configuration which has all category attributes</param>
    /// <param name="categoryTreeId">id of category tree, which represents separated hirerarchy with relations between categories</param>
    /// <param name="parentId">id of category from which new branch of hierarchy will be built. Can be null if placed at the root of category tree.</param>
    /// <param name="tenantId">Tenant id guid. A guid which uniquely identifies and isolates the data. For single
    /// tenant application this should be one hardcoded guid for whole app.</param>
    /// <param name="requestDeserializedCallback">
    /// <![CDATA[ Task<CategoryInstanceCreateRequest>(CategoryInstanceCreateRequest createRequest); ]]>
    ///
    /// This function will be called after deserializing the request from json
    /// to CategoryInstanceCreateRequest and allows adding additional validation or any other pre-processing logic.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(JsonDocument?, ProblemDetails?)> CreateCategoryInstance(
        JsonElement categoryJson,
        Guid categoryConfigurationId,
        Guid categoryTreeId,
        Guid? parentId,
        Guid? tenantId,
        Func<CategoryInstanceCreateRequest, Task<CategoryInstanceCreateRequest>>? requestDeserializedCallback = null,
        CancellationToken cancellationToken = default
    )
    {
        (CategoryInstanceCreateRequest? categoryInstanceCreateRequest, ProblemDetails? deserializationErrors)
          = await DeserializeCategoryInstanceCreateRequestFromJson(
              categoryJson,
              categoryConfigurationId,
              categoryTreeId,
              parentId,
              tenantId,
              cancellationToken
            );

        if (deserializationErrors != null)
        {
            return (null, deserializationErrors);
        }

        if (requestDeserializedCallback != null)
        {
            categoryInstanceCreateRequest = await requestDeserializedCallback(categoryInstanceCreateRequest!);
        }

        var (createdCategory, validationErrors) = await CreateCategoryInstance(
            categoryInstanceCreateRequest!, cancellationToken
        );

        if (validationErrors != null)
        {
            return (null, validationErrors);
        }

        return (SerializeEntityInstanceToJsonMultiLanguage(_mapper.Map<EntityInstanceViewModel>(createdCategory)), null);
    }

    public async Task<(CategoryViewModel, ProblemDetails)> CreateCategoryInstance(
        CategoryInstanceCreateRequest categoryCreateRequest,
        CancellationToken cancellationToken = default
    )
    {
        CategoryTree? tree = await _categoryTreeRepository.LoadAsync(
            categoryCreateRequest.CategoryTreeId,
            categoryCreateRequest.CategoryTreeId.ToString(),
            cancellationToken
        ).ConfigureAwait(false);

        if (tree == null)
        {
            return (null, new ValidationErrorResponse("CategoryTreeId", "Category tree not found"))!;
        }

        if (tree.EntityConfigurationId != categoryCreateRequest.CategoryConfigurationId)
        {
            return (null,
                new ValidationErrorResponse("CategoryConfigurationId",
                    "Category tree uses another configuration for categories"
                ))!;
        }

        EntityConfiguration? entityConfiguration = await _entityConfigurationRepository.LoadAsync(
            categoryCreateRequest.CategoryConfigurationId,
            categoryCreateRequest.CategoryConfigurationId.ToString(),
            cancellationToken
        ).ConfigureAwait(false);


        if (entityConfiguration == null)
        {
            return (null, new ValidationErrorResponse("CategoryConfigurationId", "Configuration not found"))!;
        }

        List<AttributeConfiguration> attributeConfigurations =
            await GetAttributeConfigurationsForEntityConfiguration(
                entityConfiguration,
                cancellationToken
            ).ConfigureAwait(false);


        (var categoryPath, ProblemDetails? errors) =
            await BuildCategoryPath(tree.Id, categoryCreateRequest.ParentId, cancellationToken).ConfigureAwait(false);

        if (errors != null)
        {
            return (null, errors)!;
        }

        var categoryInstance = new Category(
            Guid.NewGuid(),
            categoryCreateRequest.CategoryConfigurationId,
            _mapper.Map<List<AttributeInstance>>(categoryCreateRequest.Attributes),
            categoryCreateRequest.TenantId,
            categoryPath!,
            categoryCreateRequest.CategoryTreeId
        );

        var validationErrors = new Dictionary<string, string[]>();
        foreach (AttributeConfiguration a in attributeConfigurations)
        {
            AttributeInstance? attributeValue = categoryInstance.Attributes
                .FirstOrDefault(attr => a.MachineName == attr.ConfigurationAttributeMachineName);

            List<string> attrValidationErrors = a.ValidateInstance(attributeValue);
            if (attrValidationErrors is { Count: > 0 })
            {
                validationErrors.Add(a.MachineName, attrValidationErrors.ToArray());
            }
        }

        if (validationErrors.Count > 0)
        {
            return (null, new ValidationErrorResponse(validationErrors))!;
        }

        var mappedInstance = _mapper.Map<EntityInstance>(categoryInstance);

        ProjectionDocumentSchema schema = ProjectionDocumentSchemaFactory
            .FromEntityConfiguration(entityConfiguration, attributeConfigurations);

        IProjectionRepository projectionRepository = _projectionRepositoryFactory.GetProjectionRepository(schema);
        await projectionRepository.EnsureIndex(cancellationToken).ConfigureAwait(false);

        var saved = await _entityInstanceRepository.SaveAsync(_userInfo, mappedInstance, cancellationToken)
            .ConfigureAwait(false);
        if (!saved)
        {
            //TODO: What do we want to do with internal exceptions and unsuccessful flow?
            throw new Exception("Entity was not saved");
        }

        return (_mapper.Map<CategoryViewModel>(categoryInstance), null)!;
    }

    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "name": "Main Category",
    ///     "desprition": "Main Category description",
    ///     "entityConfigurationId": "fb80cb74-6f47-4d38-bb87-25bd820efee7",
    ///     "categoryTreeId": "65053391-9f0e-4b86-959e-2fe342e705d4",
    ///     "parentId": "3e302832-ce6b-4c41-9cf8-e2b3fdd7b01c",
    ///     "tenantId": "b6842a71-162b-411d-86e9-3ec01f909c82"
    /// }
    /// ```
    ///
    /// Where "name" and "description" are attributes machine names,
    /// "entityConfigurationId" - obviously the id of entity configuration which has all category attributes,
    /// "categoryTreeId" - guid of category tree, which represents separated hirerarchy with relations between categories
    /// "parentId" - id guid of category from which new branch of hierarchy will be built.
    /// Can be null if placed at the root of category tree.
    /// "tenantId" - tenant id guid. A guid which uniquely identifies and isolates the data. For single tenant
    /// application this should be one hardcoded guid for whole app.
    ///
    /// </remarks>
    public async Task<(CategoryInstanceCreateRequest?, ProblemDetails?)> DeserializeCategoryInstanceCreateRequestFromJson(
        JsonElement categoryJson,
        CancellationToken cancellationToken = default
    )
    {
        Guid categoryConfigurationId;
        if (categoryJson.TryGetProperty("categoryConfigurationId", out var categoryConfigurationIdJsonElement))
        {
            if (categoryConfigurationIdJsonElement.TryGetGuid(out var categoryConfigurationIdGuid))
            {
                categoryConfigurationId = categoryConfigurationIdGuid;
            }
            else
            {
                return (null, new ValidationErrorResponse("categoryConfigurationId", "Value is not a valid Guid"))!;
            }
        }
        else
        {
            return (null, new ValidationErrorResponse("categoryConfigurationId", "Value is missing"));
        }

        Guid categoryTreeId;
        if (categoryJson.TryGetProperty("categoryTreeId", out var categoryTreeIdJsonElement))
        {
            if (categoryTreeIdJsonElement.TryGetGuid(out var categoryTreeIdGuid))
            {
                categoryTreeId = categoryTreeIdGuid;
            }
            else
            {
                return (null, new ValidationErrorResponse("categoryTreeId", "Value is not a valid Guid"))!;
            }
        }
        else
        {
            return (null, new ValidationErrorResponse("categoryTreeId", "Value is missing"));
        }

        Guid? parentId = null;
        if (categoryJson.TryGetProperty("parentId", out var parentIdJsonElement))
        {
            if (parentIdJsonElement.ValueKind == JsonValueKind.Null)
            {
                parentId = null;
            }
            else if (parentIdJsonElement.TryGetGuid(out var parentIdGuid))
            {
                parentId = parentIdGuid;
            }
            else
            {
                return (null, new ValidationErrorResponse("parentId", "Value is not a valid Guid"))!;
            }
        }

        Guid? tenantId = null;
        if (categoryJson.TryGetProperty("tenantId", out var tenantIdJsonElement))
        {
            if (tenantIdJsonElement.ValueKind == JsonValueKind.Null)
            {
                tenantId = null;
            }
            else if (tenantIdJsonElement.TryGetGuid(out var tenantIdGuid))
            {
                tenantId = tenantIdGuid;
            }
            else
            {
                return (null, new ValidationErrorResponse("tenantId", "Value is not a valid Guid"))!;
            }
        }

        return await DeserializeCategoryInstanceCreateRequestFromJson(categoryJson, categoryConfigurationId, categoryTreeId, parentId, tenantId, cancellationToken);
    }

    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "name": "Main Category",
    ///     "desprition": "Main Category description"
    /// }
    /// ```
    ///
    /// Where "name" and "description" are attributes machine names.
    /// Note that this overload accepts "entityConfigurationId", "categoryTreeId", "parentId" and "tenantId" via method arguments,
    /// so they should not be in json.
    ///
    /// </remarks>
    public async Task<(CategoryInstanceCreateRequest?, ProblemDetails?)> DeserializeCategoryInstanceCreateRequestFromJson(
        JsonElement categoryJson,
        Guid categoryConfigurationId,
        Guid categoryTreeId,
        Guid? parentId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    )
    {
        EntityConfiguration? categoryConfiguration = await _entityConfigurationRepository.LoadAsync(
                categoryConfigurationId,
                categoryConfigurationId.ToString(),
                cancellationToken
            )
            .ConfigureAwait(false);

        if (categoryConfiguration == null)
        {
            return (null, new ValidationErrorResponse("CategoryConfigurationId", "CategoryConfiguration not found"))!;
        }

        List<AttributeConfiguration> attributeConfigurations = await GetAttributeConfigurationsForEntityConfiguration(
                    categoryConfiguration,
                    cancellationToken
                )
                .ConfigureAwait(false);

        return await _entityInstanceCreateUpdateRequestFromJsonDeserializer.DeserializeCategoryInstanceCreateRequest(
            categoryConfigurationId, tenantId, categoryTreeId, parentId, attributeConfigurations, categoryJson
        );
    }

    /// <summary>
    /// Returns full category tree.
    /// If notDeeperThanCategoryId is specified - returns category tree with all categories that are above or on the same lavel as a provided.
    /// <param name="treeId"></param>
    /// <param name="notDeeperThanCategoryId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>

    [SuppressMessage("Performance", "CA1806:Do not ignore method results")]
    public async Task<List<EntityTreeInstanceViewModel>> GetCategoryTreeViewAsync(
        Guid treeId,
        Guid? notDeeperThanCategoryId = null,
        CancellationToken cancellationToken = default
    )
    {
        CategoryTree? tree = await _categoryTreeRepository.LoadAsync(treeId, treeId.ToString(), cancellationToken)
            .ConfigureAwait(false);
        if (tree == null)
        {
            throw new NotFoundException("Category tree not found");
        }

        ProjectionQueryResult<EntityInstanceViewModel> treeElementsQueryResult =
            await QueryInstances(tree.EntityConfigurationId,
                new ProjectionQuery
                {
                    Filters = new List<Filter> { new("CategoryPaths.TreeId", FilterOperator.Equal, treeId) },
                    Limit = _elasticSearchQueryOptions.MaxSize
                },
                cancellationToken
            ).ConfigureAwait(false);

        var treeElements = treeElementsQueryResult.Records.Select(x => x.Document!).ToList();

        int searchedLevelPathLenght;

        if (notDeeperThanCategoryId != null)
        {
            var category = treeElements.FirstOrDefault(x => x.Id == notDeeperThanCategoryId);

            if (category == null)
            {
                throw new NotFoundException("Category not found");
            }

            searchedLevelPathLenght = category.CategoryPaths.FirstOrDefault(x => x.TreeId == treeId)!.Path.Length;

            treeElements = treeElements
                .Where(x => x.CategoryPaths.FirstOrDefault(x => x.TreeId == treeId)!.Path.Length <= searchedLevelPathLenght).ToList();
        }

        var treeViewModel = new List<EntityTreeInstanceViewModel>();

        // Go through each instance once
        foreach (EntityInstanceViewModel treeElement in treeElements
                     .OrderBy(x => x.CategoryPaths.FirstOrDefault(cp => cp.TreeId == treeId)?.Path.Length))
        {
            var treeElementViewModel = _mapper.Map<EntityTreeInstanceViewModel>(treeElement);
            var categoryPath = treeElement.CategoryPaths.FirstOrDefault(cp => cp.TreeId == treeId)?.Path;

            if (string.IsNullOrEmpty(categoryPath))
            {
                treeViewModel.Add(treeElementViewModel);
            }
            else
            {
                IEnumerable<string> categoryPathElements =
                    categoryPath.Split('/').Where(x => !string.IsNullOrEmpty(x));
                EntityTreeInstanceViewModel? currentLevel = null;
                categoryPathElements.Aggregate(treeViewModel,
                    (acc, pathComponent) =>
                    {
                        EntityTreeInstanceViewModel? parent =
                            acc.FirstOrDefault(y => y.Id.ToString() == pathComponent);
                        if (parent == null)
                        {
                            EntityInstanceViewModel? parentInstance = treeElements.FirstOrDefault(x => x.Id.ToString() == pathComponent);
                            parent = _mapper.Map<EntityTreeInstanceViewModel>(parentInstance);
                            acc.Add(parent);
                        }

                        currentLevel = parent;
                        return parent.Children;
                    }
                );
                currentLevel?.Children.Add(treeElementViewModel);
            }
        }

        return treeViewModel;
    }

    /// <summary>
    /// Returns children at one level below of the parent category in internal CategoryParentChildrenViewModel format.
    /// <param name="categoryTreeId"></param>
    /// <param name="parentId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<CategoryViewModel?>> GetSubcategories(
        Guid categoryTreeId,
        Guid? parentId,
        CancellationToken cancellationToken = default
    )
    {
        var categoryTree = await _categoryTreeRepository.LoadAsync(
            categoryTreeId, categoryTreeId.ToString(), cancellationToken
        ).ConfigureAwait(false);

        if (categoryTree == null)
        {
            throw new NotFoundException("Category tree not found");
        }

        var query = await GetSubcategoriesPrepareQuery(categoryTreeId, parentId, cancellationToken);

        var queryResult = _mapper.Map<ProjectionQueryResult<CategoryViewModel>>(
            await QueryInstances(categoryTree.EntityConfigurationId, query, cancellationToken)
        );

        return queryResult.Records.Select(x => x.Document).ToList() ?? new List<CategoryViewModel?>();
    }

    private async Task<ProjectionQuery> GetSubcategoriesPrepareQuery(
        Guid categoryTreeId,
        Guid? parentId,
        CancellationToken cancellationToken = default
    )
    {
        var categoryTree = await _categoryTreeRepository.LoadAsync(
            categoryTreeId, categoryTreeId.ToString(), cancellationToken
        ).ConfigureAwait(false);

        if (categoryTree == null)
        {
            throw new NotFoundException("Category tree not found");
        }

        ProjectionQuery query = new ProjectionQuery
        {
            Limit = _elasticSearchQueryOptions.MaxSize
        };

        if (parentId == null)
        {
            query.Filters.AddRange(

                new List<Filter>
                {
                    new Filter
                    {
                        PropertyName = $"{nameof(CategoryViewModel.CategoryPaths)}.{nameof(CategoryPath.TreeId)}",
                        Operator = FilterOperator.Equal,
                        Value = categoryTree.Id.ToString(),
                    },
                    new Filter
                    {
                        PropertyName = $"{nameof(CategoryViewModel.CategoryPaths)}.{nameof(CategoryPath.Path)}",
                        Operator = FilterOperator.Equal,
                        Value = string.Empty
                    }
                }
            );

            return query;
        }

        var category = await _entityInstanceRepository.LoadAsync(
            parentId.Value, categoryTree.EntityConfigurationId.ToString(), cancellationToken
        ).ConfigureAwait(false);

        if (category == null)
        {
            throw new NotFoundException("Category not found");
        }

        string categoryPath = category.CategoryPaths.Where(x => x.TreeId == categoryTree.Id)
            .Select(p => p.Path).FirstOrDefault()!;

        query = new ProjectionQuery
        {
            Filters = new List<Filter>
            {
                new Filter
                {
                    PropertyName = $"{nameof(CategoryViewModel.CategoryPaths)}.{nameof(CategoryPath.TreeId)}",
                    Operator = FilterOperator.Equal,
                    Value = categoryTree.Id.ToString(),
                },
                new Filter
                {
                    PropertyName = $"{nameof(CategoryViewModel.CategoryPaths)}.{nameof(CategoryPath.Path)}",
                    Operator = FilterOperator.Equal,
                    Value = categoryPath + $"/{category.Id}"
                }
            }
        };

        return query;
    }

    #endregion

    #region EntityInstance

    /// <summary>
    /// Create new entity instance from provided json string.
    /// </summary>
    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "sku": "123",
    ///     "name": "New Entity",
    ///     "entityConfigurationId": "fb80cb74-6f47-4d38-bb87-25bd820efee7",
    ///     "tenantId": "b6842a71-162b-411d-86e9-3ec01f909c82"
    /// }
    /// ```
    ///
    /// Where "sku" and "name" are attributes machine names,
    /// "entityConfigurationId" - obviously the id of entity configuration which has all attributes,
    /// "tenantId" - tenant id guid. A guid which uniquely identifies and isolates the data. For single tenant
    /// application this should be one hardcoded guid for whole app.
    ///
    /// </remarks>
    /// <param name="entityJsonString"></param>
    /// <param name="requestDeserializedCallback">
    /// <![CDATA[ Task<EntityInstanceCreateRequest>(EntityInstanceCreateRequest createRequest, bool dryRun); ]]>
    ///
    /// This function will be called after deserializing the request from json
    /// to EntityInstanceCreateRequest and allows adding additional validation or any other pre-processing logic.
    ///
    /// Note that it's important to check dryRun parameter and not make any changes to persistent store if
    /// the parameter equals to 'true'.
    /// </param>
    /// <param name="dryRun">If true, entity will only be validated but not saved to the database</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<(JsonDocument?, ProblemDetails?)> CreateEntityInstance(
        string entityJsonString,
        Func<EntityInstanceCreateRequest, bool, Task<EntityInstanceCreateRequest>>? requestDeserializedCallback = null,
        bool dryRun = false,
        bool requiredAttributesCanBeNull = false,
        CancellationToken cancellationToken = default
    )
    {
        JsonDocument entityJson = JsonDocument.Parse(entityJsonString);

        return CreateEntityInstance(
            entityJson.RootElement,
            requestDeserializedCallback,
            dryRun,
            requiredAttributesCanBeNull,
            cancellationToken
        );
    }

    /// <summary>
    /// Create new entity instance from provided json string.
    /// </summary>
    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "sku": "123",
    ///     "name": "New Entity"
    /// }
    /// ```
    ///
    /// Note that this overload accepts "entityConfigurationId" and "tenantId" via method arguments,
    /// so they should not be in json.
    ///
    /// </remarks>
    /// <param name="entityJsonString"></param>
    /// <param name="entityConfigurationId">Id of entity configuration which has all attributes</param>
    /// <param name="tenantId">Tenant id guid. A guid which uniquely identifies and isolates the data. For single
    /// tenant application this should be one hardcoded guid for whole app.</param>
    /// <param name="requestDeserializedCallback">
    /// <![CDATA[ Task<EntityInstanceCreateRequest>(EntityInstanceCreateRequest createRequest, bool dryRun); ]]>
    ///
    /// This function will be called after deserializing the request from json
    /// to EntityInstanceCreateRequest and allows adding additional validation or any other pre-processing logic.
    ///
    /// Note that it's important to check dryRun parameter and not make any changes to persistent store if
    /// the parameter equals to 'true'.
    /// </param>
    /// <param name="dryRun">If true, entity will only be validated but not saved to the database</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<(JsonDocument?, ProblemDetails?)> CreateEntityInstance(
        string entityJsonString,
        Guid entityConfigurationId,
        Guid tenantId,
        Func<EntityInstanceCreateRequest, bool, Task<EntityInstanceCreateRequest>>? requestDeserializedCallback = null,
        bool dryRun = false,
        bool requiredAttributesCanBeNull = false,
        CancellationToken cancellationToken = default
    )
    {
        JsonDocument entityJson = JsonDocument.Parse(entityJsonString);

        return CreateEntityInstance(
            entityJson.RootElement,
            entityConfigurationId,
            tenantId,
            requestDeserializedCallback,
            dryRun,
            requiredAttributesCanBeNull,
            cancellationToken
        );
    }

    /// <summary>
    /// Create new entity instance from provided json document.
    /// </summary>
    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "sku": "123",
    ///     "name": "New Entity",
    ///     "entityConfigurationId": "fb80cb74-6f47-4d38-bb87-25bd820efee7",
    ///     "tenantId": "b6842a71-162b-411d-86e9-3ec01f909c82"
    /// }
    /// ```
    ///
    /// Where "sku" and "name" are attributes machine names,
    /// "entityConfigurationId" - obviously the id of entity configuration which has all attributes,
    /// "tenantId" - tenant id guid. A guid which uniquely identifies and isolates the data. For single tenant
    /// application this should be one hardcoded guid for whole app.
    ///
    /// </remarks>
    /// <param name="entityJson"></param>
    /// <param name="requestDeserializedCallback">
    /// <![CDATA[ Task<EntityInstanceCreateRequest>(EntityInstanceCreateRequest createRequest, bool dryRun); ]]>
    ///
    /// This function will be called after deserializing the request from json
    /// to EntityInstanceCreateRequest and allows adding additional validation or any other pre-processing logic.
    ///
    /// Note that it's important to check dryRun parameter and not make any changes to persistent store if
    /// the parameter equals to 'true'.
    /// </param>
    /// <param name="dryRun">If true, entity will only be validated but not saved to the database</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(JsonDocument?, ProblemDetails?)> CreateEntityInstance(
        JsonElement entityJson,
        Func<EntityInstanceCreateRequest, bool, Task<EntityInstanceCreateRequest>>? requestDeserializedCallback = null,
        bool dryRun = false,
        bool requiredAttributesCanBeNull = false,
        CancellationToken cancellationToken = default
    )
    {
        var (entityInstanceCreateRequest, deserializationErrors) =
            await DeserializeEntityInstanceCreateRequestFromJson(entityJson, cancellationToken);

        if (deserializationErrors != null)
        {
            return (null, deserializationErrors);
        }

        return await CreateEntityInstance(
            entityJson,
            // Deserialization method ensures that EntityConfigurationId and TenantId exist and returns errors if not
            // so it's safe to use ! here
            entityInstanceCreateRequest!.EntityConfigurationId,
            entityInstanceCreateRequest.TenantId!.Value,
            requestDeserializedCallback,
            dryRun,
            requiredAttributesCanBeNull,
            cancellationToken
        );
    }

    /// <summary>
    /// Create new entity instance from provided json document.
    /// </summary>
    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "sku": "123",
    ///     "name": "New Entity"
    /// }
    /// ```
    ///
    /// Note that this overload accepts "entityConfigurationId" and "tenantId" via method arguments,
    /// so they should not be in json.
    ///
    /// </remarks>
    /// <param name="entityJson"></param>
    /// <param name="entityConfigurationId">Id of entity configuration which has all attributes</param>
    /// <param name="tenantId">Tenant id guid. A guid which uniquely identifies and isolates the data. For single
    /// tenant application this should be one hardcoded guid for whole app.</param>
    /// <param name="requestDeserializedCallback">
    /// <![CDATA[ Task<EntityInstanceCreateRequest>(EntityInstanceCreateRequest createRequest, bool dryRun); ]]>
    ///
    /// This function will be called after deserializing the request from json
    /// to EntityInstanceCreateRequest and allows adding additional validation or any other pre-processing logic.
    ///
    /// Note that it's important to check dryRun parameter and not make any changes to persistent store if
    /// the parameter equals to 'true'.
    /// </param>
    /// <param name="dryRun">If true, entity will only be validated but not saved to the database</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(JsonDocument?, ProblemDetails?)> CreateEntityInstance(
        JsonElement entityJson,
        Guid entityConfigurationId,
        Guid tenantId,
        Func<EntityInstanceCreateRequest, bool, Task<EntityInstanceCreateRequest>>? requestDeserializedCallback = null,
        bool dryRun = false,
        bool requiredAttributesCanBeNull = false,
        CancellationToken cancellationToken = default
    )
    {
        var (entityInstanceCreateRequest, deserializationErrors) = await
            DeserializeEntityInstanceCreateRequestFromJson(
                entityJson, entityConfigurationId, tenantId, cancellationToken
            );

        if (deserializationErrors != null)
        {
            return (null, deserializationErrors);
        }

        if (requestDeserializedCallback != null)
        {
            entityInstanceCreateRequest = await requestDeserializedCallback(entityInstanceCreateRequest!, dryRun);
        }

        var (createdEntity, validationErrors) = await CreateEntityInstance(
            entityInstanceCreateRequest!, dryRun, requiredAttributesCanBeNull, cancellationToken
        );

        if (validationErrors != null)
        {
            return (null, validationErrors);
        }

        return (SerializeEntityInstanceToJsonMultiLanguage(createdEntity), null);
    }

    public async Task<(EntityInstanceViewModel?, ProblemDetails?)> CreateEntityInstance(
        EntityInstanceCreateRequest entity, bool dryRun = false, bool requiredAttributesCanBeNull = false, CancellationToken cancellationToken = default
    )
    {
        EntityConfiguration? entityConfiguration = await _entityConfigurationRepository.LoadAsync(
            entity.EntityConfigurationId,
            entity.EntityConfigurationId.ToString(),
            cancellationToken
        ).ConfigureAwait(false);

        if (entityConfiguration == null)
        {
            return (null, new ValidationErrorResponse("EntityConfigurationId", "Configuration not found"))!;
        }

        List<AttributeConfiguration> attributeConfigurations =
            await GetAttributeConfigurationsForEntityConfiguration(
                entityConfiguration,
                cancellationToken
            ).ConfigureAwait(false);

        //TODO: add check for categoryPath
        var entityInstance = new EntityInstance(
            Guid.NewGuid(),
            entity.EntityConfigurationId,
            _mapper.Map<List<AttributeInstance>>(entity.Attributes),
            entity.TenantId
        );

        var validationErrors = new Dictionary<string, string[]>();
        foreach (AttributeConfiguration a in attributeConfigurations)
        {
            AttributeInstance? attributeValue = entityInstance.Attributes
                .FirstOrDefault(attr => a.MachineName == attr.ConfigurationAttributeMachineName);

            List<string> attrValidationErrors = a.ValidateInstance(attributeValue, requiredAttributesCanBeNull);
            if (attrValidationErrors is { Count: > 0 })
            {
                validationErrors.Add(a.MachineName, attrValidationErrors.ToArray());
            }

            // Note that this method updates entityConfiguration state (for serial attribute it increments the number
            // stored in externalvalues) but does not save entity configuration, we need to do that manually outside of
            // the loop
            InitializeAttributeInstanceWithExternalValuesFromEntity(entityConfiguration, a, attributeValue);
        }

        if (validationErrors.Count > 0)
        {
            return (null, new ValidationErrorResponse(validationErrors))!;
        }

        if (!dryRun)
        {
            var entityConfigurationSaved = await _entityConfigurationRepository
                .SaveAsync(_userInfo, entityConfiguration, cancellationToken)
                .ConfigureAwait(false);

            if (!entityConfigurationSaved)
            {
                throw new Exception("Entity was not saved");
            }

            ProjectionDocumentSchema schema = ProjectionDocumentSchemaFactory
                .FromEntityConfiguration(entityConfiguration, attributeConfigurations);

            IProjectionRepository projectionRepository = _projectionRepositoryFactory.GetProjectionRepository(schema);
            await projectionRepository.EnsureIndex(cancellationToken).ConfigureAwait(false);

            var entityInstanceSaved =
                await _entityInstanceRepository.SaveAsync(_userInfo, entityInstance, cancellationToken);

            if (!entityInstanceSaved)
            {
                //TODO: What do we want to do with internal exceptions and unsuccessful flow?
                throw new Exception("Entity was not saved");
            }

            return (_mapper.Map<EntityInstanceViewModel>(entityInstance), null);
        }

        return (_mapper.Map<EntityInstanceViewModel>(entityInstance), null);
    }

    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "sku": "123",
    ///     "name": "New Entity",
    ///     "entityConfigurationId": "fb80cb74-6f47-4d38-bb87-25bd820efee7",
    ///     "tenantId": "b6842a71-162b-411d-86e9-3ec01f909c82"
    /// }
    /// ```
    ///
    /// Where "sku" and "name" are attributes machine names,
    /// "entityConfigurationId" - obviously the id of entity configuration which has all attributes,
    /// "tenantId" - tenant id guid. A guid which uniquely identifies and isolates the data. For single tenant
    /// application this should be one hardcoded guid for whole app.
    ///
    /// </remarks>
    public async Task<(EntityInstanceCreateRequest?, ProblemDetails?)> DeserializeEntityInstanceCreateRequestFromJson(
        JsonElement entityJson,
        CancellationToken cancellationToken = default
    )
    {
        Guid entityConfigurationId;
        if (entityJson.TryGetProperty("entityConfigurationId", out var entityConfigurationIdJsonElement))
        {
            if (entityConfigurationIdJsonElement.TryGetGuid(out var entityConfigurationIdGuid))
            {
                entityConfigurationId = entityConfigurationIdGuid;
            }
            else
            {
                return (null, new ValidationErrorResponse("entityConfigurationId", "Value is not a valid Guid"))!;
            }
        }
        else
        {
            return (null, new ValidationErrorResponse("entityConfigurationId", "Value is missing"));
        }

        Guid tenantId;
        if (entityJson.TryGetProperty("tenantId", out var tenantIdJsonElement))
        {
            if (tenantIdJsonElement.TryGetGuid(out var tenantIdGuid))
            {
                tenantId = tenantIdGuid;
            }
            else
            {
                return (null, new ValidationErrorResponse("tenantId", "Value is not a valid Guid"))!;
            }
        }
        else
        {
            return (null, new ValidationErrorResponse("tenantId", "Value is missing"));
        }

        return await DeserializeEntityInstanceCreateRequestFromJson(
            entityJson, entityConfigurationId, tenantId, cancellationToken
        );
    }

    /// <remarks>
    /// Use following json format:
    ///
    /// ```
    /// {
    ///     "sku": "123",
    ///     "name": "New Entity"
    /// }
    /// ```
    ///
    /// Note that this overload accepts "entityConfigurationId" and "tenantId" via method arguments,
    /// so they should not be in json.
    ///
    /// </remarks>
    public async Task<(EntityInstanceCreateRequest?, ProblemDetails?)> DeserializeEntityInstanceCreateRequestFromJson(
        JsonElement entityJson,
        Guid entityConfigurationId,
        Guid tenantId,
        CancellationToken cancellationToken = default
    )
    {
        EntityConfiguration? entityConfiguration = await _entityConfigurationRepository.LoadAsync(
                entityConfigurationId,
                entityConfigurationId.ToString(),
                cancellationToken
            )
            .ConfigureAwait(false);

        if (entityConfiguration == null)
        {
            return (null, new ValidationErrorResponse("EntityConfigurationId", "EntityConfiguration not found"))!;
        }

        List<AttributeConfiguration> attributeConfigurations =
            await GetAttributeConfigurationsForEntityConfiguration(
                    entityConfiguration,
                    cancellationToken
                )
                .ConfigureAwait(false);

        return await _entityInstanceCreateUpdateRequestFromJsonDeserializer.DeserializeEntityInstanceCreateRequest(
            entityConfigurationId, tenantId, attributeConfigurations, entityJson
        );
    }

    public async Task<EntityInstanceViewModel?> GetEntityInstance(Guid id, string partitionKey)
    {
        EntityInstance? entityInstance = await _entityInstanceRepository.LoadAsync(id, partitionKey);

        return _mapper.Map<EntityInstanceViewModel?>(entityInstance);
    }

    public async Task<JsonDocument> GetEntityInstanceJsonMultiLanguage(Guid id, string partitionKey)
    {
        EntityInstanceViewModel? entityInstanceViewModel = await GetEntityInstance(id, partitionKey);

        return SerializeEntityInstanceToJsonMultiLanguage(entityInstanceViewModel);
    }

    public async Task<JsonDocument> GetEntityInstanceJsonSingleLanguage(
        Guid id,
        string partitionKey,
        string language,
        string fallbackLanguage = "en-US")
    {
        EntityInstanceViewModel? entityInstanceViewModel = await GetEntityInstance(id, partitionKey);

        return SerializeEntityInstanceToJsonSingleLanguage(entityInstanceViewModel, language, fallbackLanguage);
    }

    public JsonDocument SerializeEntityInstanceToJsonMultiLanguage(EntityInstanceViewModel? entityInstanceViewModel)
    {
        var serializerOptions = new JsonSerializerOptions(_jsonSerializerOptions);
        serializerOptions.Converters.Add(new LocalizedStringMultiLanguageSerializer());
        serializerOptions.Converters.Add(new EntityInstanceViewModelToJsonSerializer());

        return JsonSerializer.SerializeToDocument(entityInstanceViewModel, serializerOptions);
    }

    public JsonDocument SerializeEntityInstanceToJsonSingleLanguage(
        EntityInstanceViewModel? entityInstanceViewModel, string language, string fallbackLanguage = "en-US"
    )
    {
        var serializerOptions = new JsonSerializerOptions(_jsonSerializerOptions);
        serializerOptions.Converters.Add(new LocalizedStringSingleLanguageSerializer(language, fallbackLanguage));
        serializerOptions.Converters.Add(new EntityInstanceViewModelToJsonSerializer());

        return JsonSerializer.SerializeToDocument(entityInstanceViewModel, serializerOptions);
    }

    public async Task<(EntityInstanceViewModel, ProblemDetails)> UpdateEntityInstance(
        string partitionKey,
        EntityInstanceUpdateRequest updateRequest,
        bool dryRun = false,
        bool requiredAttributesCanBeNull = false,
        CancellationToken cancellationToken = default
    )
    {
        EntityInstance? entityInstance =
            await _entityInstanceRepository.LoadAsync(updateRequest.Id, partitionKey, cancellationToken);

        if (entityInstance == null)
        {
            return (null, new ValidationErrorResponse(nameof(updateRequest.Id), "Entity instance not found"))!;
        }

        EntityConfiguration? entityConfiguration = await _entityConfigurationRepository.LoadAsync(
            entityInstance.EntityConfigurationId,
            entityInstance.EntityConfigurationId.ToString(),
            cancellationToken
        );

        if (entityConfiguration == null)
        {
            return (null,
                new ValidationErrorResponse(nameof(updateRequest.EntityConfigurationId),
                    "Entity configuration not found"
                ))!;
        }

        List<AttributeConfiguration> entityConfigurationAttributeConfigurations =
            await GetAttributeConfigurationsForEntityConfiguration(
                entityConfiguration,
                cancellationToken
            );

        var validationErrors = new Dictionary<string, string[]>();

        if (updateRequest.AttributeMachineNamesToRemove != null)
        {
            foreach (var attributeMachineNameToRemove in updateRequest.AttributeMachineNamesToRemove)
            {
                AttributeConfiguration? attrConfiguration = entityConfigurationAttributeConfigurations
                    .First(c => c.MachineName == attributeMachineNameToRemove);
                updateRequest.AttributesToAddOrUpdate.RemoveAll(a =>
                    a.ConfigurationAttributeMachineName == attributeMachineNameToRemove);

                if (requiredAttributesCanBeNull)
                {
                    entityInstance.RemoveAttributeInstance(attributeMachineNameToRemove);
                    continue;
                }

                // validation against null will check if the attribute is required
                List<string> errors = attrConfiguration.ValidateInstance(null);

                if (errors.Count == 0)
                {
                    entityInstance.RemoveAttributeInstance(attributeMachineNameToRemove);
                }
                else
                {
                    validationErrors.Add(attributeMachineNameToRemove, errors.ToArray());
                }
            }
        }

        // Add or update attributes
        foreach (AttributeInstanceCreateUpdateRequest? newAttributeRequest in updateRequest.AttributesToAddOrUpdate)
        {
            AttributeConfiguration? attrConfig = entityConfigurationAttributeConfigurations
                .FirstOrDefault(c => c.MachineName == newAttributeRequest.ConfigurationAttributeMachineName);

            if (attrConfig == null)
            {
                continue;
            }

            var newAttribute = _mapper.Map<AttributeInstance>(newAttributeRequest);
            List<string> errors = attrConfig.ValidateInstance(newAttribute, requiredAttributesCanBeNull);

            if (errors.Count == 0)
            {
                AttributeInstance? currentAttribute = entityInstance.Attributes.FirstOrDefault(x =>
                    x.ConfigurationAttributeMachineName == newAttributeRequest.ConfigurationAttributeMachineName
                );
                if (currentAttribute != null)
                {
                    if (!newAttribute.Equals(currentAttribute))
                    {
                        var updateExternalValuesErrors = UpdateEntityExternalValuesDuringInstanceUpdate(entityConfiguration, attrConfig, newAttribute);

                        if (updateExternalValuesErrors == null)
                        {
                            entityInstance.UpdateAttributeInstance(newAttribute);
                        }
                        else
                        {
                            validationErrors.Add(newAttribute.ConfigurationAttributeMachineName, updateExternalValuesErrors.ToArray());
                        }
                    }
                }
                else
                {
                    InitializeAttributeInstanceWithExternalValuesFromEntity(entityConfiguration, attrConfig, newAttribute);

                    entityInstance.AddAttributeInstance(
                        _mapper.Map<AttributeInstance>(newAttributeRequest)
                    );
                }
            }
            else
            {
                validationErrors.Add(newAttribute.ConfigurationAttributeMachineName, errors.ToArray());
            }
        }

        if (validationErrors.Count != 0)
        {
            return (null, new ValidationErrorResponse(validationErrors))!;
        }

        if (!dryRun)
        {
            var entityConfigurationSaved = await _entityConfigurationRepository
                .SaveAsync(_userInfo, entityConfiguration, cancellationToken)
                .ConfigureAwait(false);

            var entityInstanceSaved = await _entityInstanceRepository
                .SaveAsync(_userInfo, entityInstance, cancellationToken)
                .ConfigureAwait(false);
            if (!entityInstanceSaved || !entityConfigurationSaved)
            {
                //TODO: Throw a error when ready
            }
        }

        return (_mapper.Map<EntityInstanceViewModel>(entityInstance), null)!;
    }

    /// <summary>
    /// Returns records in internal EntityInstanceViewModel format - use this is library is used by .net code.
    /// That way you will have full control over attributes and will be able to convert them to
    /// create/update request models for updating the entity.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ProjectionQueryResult<EntityInstanceViewModel>> QueryInstances(
        Guid entityConfigurationId,
        ProjectionQuery query,
        CancellationToken cancellationToken = default
    )
    {
        EntityConfiguration entityConfiguration = await _entityConfigurationRepository.LoadAsyncOrThrowNotFound(
            entityConfigurationId,
            entityConfigurationId.ToString(),
            cancellationToken
        ).ConfigureAwait(false);

        List<AttributeConfiguration> attributes = await GetAttributeConfigurationsForEntityConfiguration(
            entityConfiguration,
            cancellationToken
        ).ConfigureAwait(false);

        ProjectionDocumentSchema schema = ProjectionDocumentSchemaFactory
            .FromEntityConfiguration(entityConfiguration, attributes);

        IProjectionRepository projectionRepository = _projectionRepositoryFactory.GetProjectionRepository(schema);

        ProjectionQueryResult<Dictionary<string, object?>> results = await projectionRepository
            .Query(query, entityConfiguration.Id.ToString(), cancellationToken)
            .ConfigureAwait(false);

        return results.TransformResultDocuments(
            r => _entityInstanceFromDictionaryDeserializer.Deserialize(attributes, r)
        );
    }

    /// <summary>
    /// Returns records in json serialized format.
    /// LocalizedStrings are returned as objects whose property names are language identifiers
    /// and property values are language translation strings.
    ///
    /// EntityInstance with:
    ///
    /// - one text attribute of type LocalizedString "productName"
    /// - one number attribute of type Number "price"
    ///
    /// will be returned in following json format:
    ///
    /// ```
    /// {
    ///   "productName": {
    ///     "en-US": "Terraforming Mars",
    ///     "ru-RU": "Покорение Марса"
    ///   },
    ///   "price": 100
    /// }
    /// ```
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ProjectionQueryResult<JsonDocument>> QueryInstancesJsonMultiLanguage(
        Guid entityConfigurationId,
        ProjectionQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var results = await QueryInstances(
            entityConfigurationId,
            query,
            cancellationToken
        );

        var serializerOptions = new JsonSerializerOptions(_jsonSerializerOptions);
        serializerOptions.Converters.Add(new EntityInstanceViewModelToJsonSerializer());
        serializerOptions.Converters.Add(new LocalizedStringMultiLanguageSerializer());

        return results.TransformResultDocuments(
            r => JsonSerializer.SerializeToDocument(r, serializerOptions)
        );
    }

    /// <summary>
    /// Returns records in json serialized format.
    /// LocalizedStrings are converted to a single language string of the language passed in parameters.
    ///
    /// EntityInstance with:
    ///
    /// - one text attribute of type LocalizedString "productName"
    /// - one number attribute of type Number "price"
    ///
    /// will be returned in following json format:
    ///
    /// ```
    /// {
    ///   "productName": "Terraforming Mars",
    ///   "price": 100
    /// }
    /// ```
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="query"></param>
    /// <param name="language">Language to use from all localized strings. Only this language strings will be returned.</param>
    /// <param name="fallbackLanguage">If main language will not be found, this language will be tried. Defaults to en-US.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ProjectionQueryResult<JsonDocument>> QueryInstancesJsonSingleLanguage(
        Guid entityConfigurationId,
        ProjectionQuery query,
        string language = "en-US",
        string fallbackLanguage = "en-US",
        CancellationToken cancellationToken = default
    )
    {
        var results = await QueryInstances(
            entityConfigurationId,
            query,
            cancellationToken
        );

        var serializerOptions = new JsonSerializerOptions(_jsonSerializerOptions);
        serializerOptions.Converters.Add(new EntityInstanceViewModelToJsonSerializer());
        serializerOptions.Converters.Add(new LocalizedStringSingleLanguageSerializer(language, fallbackLanguage));

        return results.TransformResultDocuments(
            r => JsonSerializer.SerializeToDocument(r, serializerOptions)
        );
    }

    public async Task<(EntityInstanceViewModel, ProblemDetails)> UpdateCategoryPath(Guid entityInstanceId,
        string entityInstancePartitionKey, Guid treeId, Guid? newParentId, CancellationToken cancellationToken = default)
    {
        EntityInstance? entityInstance = await _entityInstanceRepository
            .LoadAsync(entityInstanceId, entityInstancePartitionKey, cancellationToken).ConfigureAwait(false);
        if (entityInstance == null)
        {
            return (null, new ValidationErrorResponse(nameof(entityInstanceId), "Instance not found"))!;
        }

        (var newCategoryPath, ProblemDetails? errors) =
            await BuildCategoryPath(treeId, newParentId, cancellationToken).ConfigureAwait(false);

        if (errors != null)
        {
            return (null, errors)!;
        }

        entityInstance.ChangeCategoryPath(treeId, newCategoryPath ?? "");
        var saved = await _entityInstanceRepository.SaveAsync(_userInfo, entityInstance, cancellationToken)
            .ConfigureAwait(false);
        if (!saved)
        {
            //TODO: What do we want to do with internal exceptions and unsuccessful flow?
            throw new Exception("Entity was not saved");
        }

        return (_mapper.Map<EntityInstanceViewModel>(entityInstance), null)!;
    }

    private async Task<List<AttributeConfiguration>> GetAttributeConfigurationsForEntityConfiguration(
        EntityConfiguration entityConfiguration, CancellationToken cancellationToken = default
    )
    {
        var attributeConfigurations = new List<AttributeConfiguration>();

        foreach (EntityConfigurationAttributeReference attributeReference in entityConfiguration.Attributes)
        {
            attributeConfigurations.Add(
                await _attributeConfigurationRepository.LoadAsyncOrThrowNotFound(
                    attributeReference.AttributeConfigurationId,
                    attributeReference.AttributeConfigurationId.ToString(),
                    cancellationToken
                )
            );
        }

        return attributeConfigurations;
    }

    #endregion
}

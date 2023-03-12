using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using AutoMapper;

using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Models.ViewModels.Attributes;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Persistence;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
    private readonly AggregateRepository<EntityInstance> _entityInstanceRepository;
    private readonly ILogger<EAVService> _logger;
    private readonly IMapper _mapper;
    private readonly ProjectionRepositoryFactory _projectionRepositoryFactory;

    private readonly EventUserInfo _userInfo;

    public EAVService(
        ILogger<EAVService> logger,
        IMapper mapper,
        AggregateRepositoryFactory aggregateRepositoryFactory,
        ProjectionRepositoryFactory projectionRepositoryFactory,
        EventUserInfo userInfo
    )
    {
        _logger = logger;
        _mapper = mapper;
        _aggregateRepositoryFactory = aggregateRepositoryFactory;
        _projectionRepositoryFactory = projectionRepositoryFactory;
        _userInfo = userInfo;

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
    }

    private void EnsureAttributeMachineNameIsAdded(AttributeConfigurationCreateUpdateRequest attributeRequest)
    {
        if (string.IsNullOrWhiteSpace(attributeRequest.MachineName))
        {
            var machineName =
                attributeRequest.Name
                    .FirstOrDefault(x => x.CultureInfoId == new CultureInfo("EN-us").LCID)
                    ?.String
                ?? attributeRequest.Name.First().String;

            // remove spec symbols
            machineName = machineName.Replace(" ", "_");
            var specSymbolsRegex = new Regex("[^\\d\\w_]*", RegexOptions.None, TimeSpan.FromMilliseconds(1));
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

    private async Task<(string?, ProblemDetails?)> BuildCategoryPathAsync(Guid treeId, Guid? parentId,
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

        await _attributeConfigurationProjectionRepository.EnsureIndex(cancellationToken).ConfigureAwait(false);

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

            if (!(attribute is EntityAttributeConfigurationCreateUpdateReferenceRequest))
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

        var entityConfigurationExistingAttributes =
            await GetAttributeConfigurationsForEntityConfiguration(entityConfiguration, cancellationToken);

        if (entityConfiguration == null)
        {
            return (null,
                new ValidationErrorResponse(nameof(entityUpdateRequest.Id), "Entity configuration not found"))!;
        }

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

    public async Task<(CategoryViewModel, ProblemDetails)> CreateCategoryInstanceAsync(
        CategoryInstanceCreateRequest entity,
        CancellationToken cancellationToken = default
    )
    {
        CategoryTree? tree = await _categoryTreeRepository.LoadAsync(
            entity.CategoryTreeId,
            entity.CategoryTreeId.ToString(),
            cancellationToken
        ).ConfigureAwait(false);

        if (tree == null)
        {
            return (null, new ValidationErrorResponse("CategoryTreeId", "Category tree not found"))!;
        }

        if (tree.EntityConfigurationId != entity.CategoryConfigurationId)
        {
            return (null,
                new ValidationErrorResponse("CategoryConfigurationId",
                    "Category tree uses another configuration for categories"
                ))!;
        }

        EntityConfiguration? entityConfiguration = await _entityConfigurationRepository.LoadAsync(
            entity.CategoryConfigurationId,
            entity.CategoryConfigurationId.ToString(),
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
            await BuildCategoryPathAsync(tree.Id, entity.ParentId, cancellationToken).ConfigureAwait(false);

        if (errors != null)
        {
            return (null, errors)!;
        }

        var entityInstance = new Category(
            Guid.NewGuid(),
            entity.CategoryConfigurationId,
            _mapper.Map<List<AttributeInstance>>(entity.Attributes),
            entity.TenantId,
            categoryPath,
            entity.CategoryTreeId
        );

        var validationErrors = new Dictionary<string, string[]>();
        foreach (AttributeConfiguration a in attributeConfigurations)
        {
            AttributeInstance? attributeValue = entityInstance.Attributes
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

        var mappedInstance = _mapper.Map<EntityInstance>(entityInstance);

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

        return (_mapper.Map<CategoryViewModel>(entityInstance), null)!;
    }

    [SuppressMessage("Performance", "CA1806:Do not ignore method results")]
    public async Task<List<EntityTreeInstanceViewModel>> GetCategoryTreeViewAsync(
        Guid treeId,
        CancellationToken cancellationToken = default
    )
    {
        CategoryTree? tree = await _categoryTreeRepository.LoadAsync(treeId, treeId.ToString(), cancellationToken)
            .ConfigureAwait(false);
        if (tree == null)
        {
            throw new NotFoundException("Category tree not found");
        }

        ProjectionQueryResult<EntityInstanceViewModel> treeElements =
            await QueryInstances(tree.EntityConfigurationId,
                new ProjectionQuery
                {
                    Filters = new List<Filter> { new("CategoryPaths.TreeId", FilterOperator.Equal, treeId) }
                },
                cancellationToken
            ).ConfigureAwait(false);

        var treeViewModel = new List<EntityTreeInstanceViewModel>();

        // Go through each instance once
        foreach (EntityInstanceViewModel treeElement in treeElements.Records
                     .Select(x => x.Document!)
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
                            EntityInstanceViewModel? parentInstance = treeElements.Records.Select(x => x.Document)
                                .FirstOrDefault(x => x.Id.ToString() == pathComponent);
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

    #endregion

    #region EntityInstance

    public async Task<(EntityInstanceViewModel, ProblemDetails)> CreateEntityInstance(
        EntityInstanceCreateRequest entity, CancellationToken cancellationToken = default
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

            List<string> attrValidationErrors = a.ValidateInstance(attributeValue);
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

        var entityConfigurationSaved = await _entityConfigurationRepository
            .SaveAsync(_userInfo, entityConfiguration, cancellationToken).ConfigureAwait(false);

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

        return (_mapper.Map<EntityInstanceViewModel>(entityInstance), null)!;
    }

    public async Task<EntityInstanceViewModel> GetEntityInstance(Guid id, string partitionKey)
    {
        EntityInstance? entityInstance = await _entityInstanceRepository.LoadAsync(id, partitionKey);

        return _mapper.Map<EntityInstanceViewModel>(entityInstance);
    }

    public async Task<(EntityInstanceViewModel, ProblemDetails)> UpdateEntityInstance(string partitionKey,
        EntityInstanceUpdateRequest updateRequest, CancellationToken cancellationToken)
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

        // Remove attributes
        IEnumerable<AttributeInstance> attributesToRemove = entityInstance.Attributes
            .ExceptBy(
                updateRequest.Attributes.Select(x => x.ConfigurationAttributeMachineName),
                x => x.ConfigurationAttributeMachineName
            );

        foreach (AttributeInstance? attribute in attributesToRemove)
        {
            AttributeConfiguration? attrConfiguration = entityConfigurationAttributeConfigurations
                .First(c => c.MachineName == attribute.ConfigurationAttributeMachineName);

            // validation against null will check if the attribute is required
            List<string> errors = attrConfiguration.ValidateInstance(null);

            if (errors.Count == 0)
            {
                entityInstance.RemoveAttributeInstance(attribute.ConfigurationAttributeMachineName);
            }
            else
            {
                validationErrors.Add(attribute.ConfigurationAttributeMachineName, errors.ToArray());
            }
        }

        // Add or update attributes
        foreach (AttributeInstanceCreateUpdateRequest? newAttributeRequest in updateRequest.Attributes)
        {
            AttributeConfiguration? attrConfig = entityConfigurationAttributeConfigurations
                .FirstOrDefault(c => c.MachineName == newAttributeRequest.ConfigurationAttributeMachineName);

            if (attrConfig == null)
            {
                continue;
            }

            var newAttribute = _mapper.Map<AttributeInstance>(newAttributeRequest);
            List<string> errors = attrConfig.ValidateInstance(newAttribute);

            if (errors.Count == 0)
            {
                AttributeInstance? currentAttribute = entityInstance.Attributes.FirstOrDefault(x =>
                    x.ConfigurationAttributeMachineName == newAttributeRequest.ConfigurationAttributeMachineName
                );
                if (currentAttribute != null)
                {
                    if (!newAttribute.Equals(currentAttribute))
                    {
                        entityInstance.UpdateAttributeInstance(newAttribute);
                    }
                }
                else
                {
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

        var saved = await _entityInstanceRepository.SaveAsync(_userInfo, entityInstance, cancellationToken);
        if (!saved)
        {
            //TODO: Throw a error when ready
        }

        return (_mapper.Map<EntityInstanceViewModel>(entityInstance), null)!;
    }

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
            .Query(query, entityConfigurationId.ToString(), cancellationToken)
            .ConfigureAwait(false);

        return results.TransformResultDocuments(
            r => _entityInstanceFromDictionaryDeserializer.Deserialize(attributes, r)
        );
    }

    public async Task<(EntityInstanceViewModel, ProblemDetails)> UpdateCategoryPathAsync(Guid itemId,
        string itemPartitionKey, Guid treeId, Guid newParentId, CancellationToken cancellationToken)
    {
        EntityInstance? item = await _entityInstanceRepository
            .LoadAsync(itemId, itemPartitionKey, cancellationToken).ConfigureAwait(false);
        if (item == null)
        {
            return (null, new ValidationErrorResponse(nameof(itemId), "Item not found"))!;
        }

        (var newCategoryPath, ProblemDetails? errors) =
            await BuildCategoryPathAsync(treeId, newParentId, cancellationToken).ConfigureAwait(false);
        if (errors != null)
        {
            return (null, errors)!;
        }

        CategoryPath? itemCategoryPath = item.CategoryPaths.FirstOrDefault(x => x.TreeId == treeId);
        item.ChangeCategoryPath(treeId, newCategoryPath ?? "");
        var saved = await _entityInstanceRepository.SaveAsync(_userInfo, item, cancellationToken)
            .ConfigureAwait(false);
        if (!saved)
        {
            //TODO: What do we want to do with internal exceptions and unsuccessful flow?
            throw new Exception("Entity was not saved");
        }

        return (_mapper.Map<EntityInstanceViewModel>(item), null)!;
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

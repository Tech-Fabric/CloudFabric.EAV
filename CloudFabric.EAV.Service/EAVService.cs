using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

using AutoMapper;

using CloudFabric.EAV.Domain.GeneratedValues;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;
using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Models.ViewModels.Attributes;
using CloudFabric.EAV.Service.Serialization;
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

public class EAVService
{
    private readonly ILogger<EAVService> _logger;
    private readonly IMapper _mapper;
    internal readonly JsonSerializerOptions JsonSerializerOptions;
    private readonly EventUserInfo _userInfo;

    internal readonly AggregateRepository<EntityConfiguration> EntityConfigurationRepository;
    internal readonly AggregateRepository<EntityInstance> EntityInstanceRepository;

    internal readonly AggregateRepository<AttributeConfiguration> AttributeConfigurationRepository;

    internal readonly ProjectionRepositoryFactory _projectionRepositoryFactory;

    private readonly IProjectionRepository<AttributeConfigurationProjectionDocument>
        _attributeConfigurationProjectionRepository;
    private readonly IProjectionRepository<EntityConfigurationProjectionDocument>
        _entityConfigurationProjectionRepository;



    private readonly InstanceFromDictionaryDeserializer
        _entityInstanceFromDictionaryDeserializer;
    private readonly EntityInstanceCreateUpdateRequestFromJsonDeserializer
        _entityInstanceCreateUpdateRequestFromJsonDeserializer;

    internal readonly ValueAttributeService _valueAttributeService;

    public EAVService(
        ILogger<EAVService> logger,
        IMapper mapper,
        JsonSerializerOptions jsonSerializerOptions,
        AggregateRepositoryFactory aggregateRepositoryFactory,
        ProjectionRepositoryFactory projectionRepositoryFactory,
        EventUserInfo userInfo,
        ValueAttributeService valueAttributeService)
    {
        _logger = logger;
        _mapper = mapper;
        JsonSerializerOptions = jsonSerializerOptions;

        _projectionRepositoryFactory = projectionRepositoryFactory;

        _userInfo = userInfo;

        AttributeConfigurationRepository = aggregateRepositoryFactory
            .GetAggregateRepository<AttributeConfiguration>();
        EntityConfigurationRepository = aggregateRepositoryFactory
            .GetAggregateRepository<EntityConfiguration>();
        EntityInstanceRepository = aggregateRepositoryFactory
            .GetAggregateRepository<EntityInstance>();

        _attributeConfigurationProjectionRepository = _projectionRepositoryFactory
            .GetProjectionRepository<AttributeConfigurationProjectionDocument>();
        _entityConfigurationProjectionRepository = _projectionRepositoryFactory
            .GetProjectionRepository<EntityConfigurationProjectionDocument>();

        _entityInstanceFromDictionaryDeserializer = new InstanceFromDictionaryDeserializer(_mapper);

        _entityInstanceCreateUpdateRequestFromJsonDeserializer =
            new EntityInstanceCreateUpdateRequestFromJsonDeserializer(
                AttributeConfigurationRepository, jsonSerializerOptions
            );

        _valueAttributeService = valueAttributeService;
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

        List<string> machineNames = new List<string>();
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

    private async Task<ProjectionQueryResult<AttributeConfigurationListItemViewModel>> GetAttributesByIds(
        List<Guid> attributesIds, CancellationToken cancellationToken)
    {
        // create attributes filter
        Filter attributeIdFilter = new Filter(nameof(AttributeConfigurationProjectionDocument.Id),
            FilterOperator.Equal,
            attributesIds[0]);

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

    #region EntityConfiguration

    public async Task<EntityConfigurationViewModel> GetEntityConfiguration(Guid id)
    {
        EntityConfiguration? entityConfiguration = await EntityConfigurationRepository.LoadAsync(id, id.ToString());

        return _mapper.Map<EntityConfigurationViewModel>(entityConfiguration);
    }

    public async Task<EntityConfigurationWithAttributesViewModel> GetEntityConfigurationWithAttributes(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var entityConfiguration = await EntityConfigurationRepository.LoadAsyncOrThrowNotFound(
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

        await AttributeConfigurationRepository.SaveAsync(_userInfo, attribute, cancellationToken)
            .ConfigureAwait(false);

        return (_mapper.Map<AttributeConfigurationViewModel>(attribute), null);
    }



    public async Task<AttributeConfigurationViewModel> GetAttribute(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        AttributeConfiguration attribute = await AttributeConfigurationRepository
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
        AttributeConfiguration? attribute = await AttributeConfigurationRepository
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

        await AttributeConfigurationRepository.SaveAsync(_userInfo, attribute, cancellationToken)
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
            );
        }

        foreach (EntityAttributeConfigurationCreateUpdateRequest attribute in entityConfigurationCreateRequest
                     .Attributes
                     .Where(x => x is EntityAttributeConfigurationCreateUpdateReferenceRequest))
        {
            var requestAttribute = (EntityAttributeConfigurationCreateUpdateReferenceRequest)attribute;

            AttributeConfiguration attributeConfiguration = await AttributeConfigurationRepository
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
                );
            }
        }

        var allCreatedAttributes = new List<AttributeConfigurationViewModel>();
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
                    allCreatedAttributes.Add(attrCreated);
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

        await _valueAttributeService.InitializeEntityConfigurationGeneratedValues(entityConfiguration.Id, allCreatedAttributes);

        await EntityConfigurationRepository.SaveAsync(
            _userInfo,
            entityConfiguration,
            cancellationToken
        ).ConfigureAwait(false);

        await EnsureProjectionIndexForEntityConfiguration(entityConfiguration);

        return (_mapper.Map<EntityConfigurationViewModel>(entityConfiguration), null);
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
            );
        }

        EntityConfiguration? entityConfiguration = await EntityConfigurationRepository.LoadAsync(
            entityUpdateRequest.Id,
            entityUpdateRequest.Id.ToString(),
            cancellationToken
        ).ConfigureAwait(false);

        if (entityConfiguration == null)
        {
            return (null,
                new ValidationErrorResponse(nameof(entityUpdateRequest.Id), "Entity configuration not found"));
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

                AttributeConfiguration? attributeConfiguration = await AttributeConfigurationRepository.LoadAsync(
                    attributeReferenceUpdate.AttributeConfigurationId,
                    attributeReferenceUpdate.AttributeConfigurationId.ToString(),
                    cancellationToken
                ).ConfigureAwait(false);

                if (attributeConfiguration is
                    {
                        IsDeleted: false
                    })
                {
                    if (attributeShouldBeAdded)
                    {
                        entityConfiguration.AddAttribute(attributeConfiguration.Id);
                    }

                    reservedAttributes.Add(attributeConfiguration.Id);

                    await _valueAttributeService.InitializeGeneratedValue(entityConfiguration.Id, attributeConfiguration);
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

                        await _valueAttributeService.InitializeGeneratedValue(entityConfiguration.Id, attributeCreated);
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

        //await _entityConfigurationProjectionRepository.EnsureIndex(cancellationToken).ConfigureAwait(false);
        await EntityConfigurationRepository.SaveAsync(_userInfo, entityConfiguration, cancellationToken)
            .ConfigureAwait(false);

        await EnsureProjectionIndexForEntityConfiguration(entityConfiguration);

        return (_mapper.Map<EntityConfigurationViewModel>(entityConfiguration), null);
    }

    private async Task EnsureProjectionIndexForEntityConfiguration(EntityConfiguration entityConfiguration)
    {
        // when entity configuration is created or updated, we need to create a projections index for it. EnsureIndex
        // will just create a record that such index is needed. Then, it will be picked up by background processor
        List<AttributeConfiguration> attributeConfigurations = await GetAttributeConfigurationsForEntityConfiguration(
            entityConfiguration
        );
        var schema = ProjectionDocumentSchemaFactory
            .FromEntityConfiguration(entityConfiguration, attributeConfigurations);
        IProjectionRepository projectionRepository = _projectionRepositoryFactory.GetProjectionRepository(schema);
        await projectionRepository.EnsureIndex();
    }

    public async Task<(EntityConfigurationViewModel?, ProblemDetails?)> AddAttributeToEntityConfiguration(
        Guid attributeId,
        Guid entityConfigurationId,
        CancellationToken cancellationToken = default
    )
    {
        AttributeConfiguration attributeConfiguration = await AttributeConfigurationRepository
            .LoadAsyncOrThrowNotFound(
                attributeId,
                attributeId.ToString(),
                cancellationToken
            ).ConfigureAwait(false);

        if (attributeConfiguration.IsDeleted)
        {
            return (null, new ValidationErrorResponse(nameof(attributeId), "Attribute not found"));
        }

        EntityConfiguration entityConfiguration = await EntityConfigurationRepository.LoadAsyncOrThrowNotFound(
            entityConfigurationId,
            entityConfigurationId.ToString(),
            cancellationToken
        ).ConfigureAwait(false);

        if (entityConfiguration.Attributes.Any(x => x.AttributeConfigurationId == attributeConfiguration.Id))
        {
            return (null, new ValidationErrorResponse(nameof(attributeId), "Attribute has already been added"));
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

        await EntityConfigurationRepository.SaveAsync(_userInfo, entityConfiguration, cancellationToken)
            .ConfigureAwait(false);

        await _valueAttributeService.InitializeGeneratedValue(entityConfigurationId, attributeConfiguration);

        return (_mapper.Map<EntityConfigurationViewModel>(entityConfiguration), null);
    }

    public async Task<(AttributeConfigurationViewModel, ProblemDetails)> CreateAttributeAndAddToEntityConfiguration(
        AttributeConfigurationCreateUpdateRequest attributeConfigurationCreateUpdateRequest,
        Guid entityConfigurationId,
        CancellationToken cancellationToken = default
    )
    {
        EnsureAttributeMachineNameIsAdded(attributeConfigurationCreateUpdateRequest);

        EntityConfiguration entityConfiguration = await EntityConfigurationRepository.LoadAsyncOrThrowNotFound(
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

        await EntityConfigurationRepository.SaveAsync(_userInfo, entityConfiguration, cancellationToken);

        await _valueAttributeService.InitializeGeneratedValue(entityConfigurationId, createdAttribute);

        return (createdAttribute, null)!;
    }

    public async Task DeleteAttributesFromEntityConfiguration(List<Guid> attributesIds, Guid entityConfigurationId,
        CancellationToken cancellationToken = default)
    {
        EntityConfiguration entityConfiguration = await EntityConfigurationRepository.LoadAsyncOrThrowNotFound(
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

        await EntityConfigurationRepository.SaveAsync(_userInfo, entityConfiguration, cancellationToken);
    }

    public async Task DeleteAttributes(List<Guid> attributesIds, CancellationToken cancellationToken = default)
    {
        foreach (Guid attributeId in attributesIds)
        {
            AttributeConfiguration? attributeConfiguration = await AttributeConfigurationRepository.LoadAsync(
                attributeId,
                attributeId.ToString(),
                cancellationToken
            );

            if (attributeConfiguration is
                {
                    IsDeleted: false
                })
            {
                attributeConfiguration.Delete();

                await AttributeConfigurationRepository
                    .SaveAsync(_userInfo, attributeConfiguration, cancellationToken).ConfigureAwait(false);
            }
        }

        var filterPropertyName = string.Concat(
            nameof(EntityConfigurationProjectionDocument.Attributes),
            ".",
            nameof(AttributeConfigurationReference.AttributeConfigurationId
            )
        );

        Filter filters = new Filter(filterPropertyName, FilterOperator.Equal, attributesIds[0]);

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
                var saved = await AttributeConfigurationRepository.SaveAsync(_userInfo, defaultTypeConfig, cancellationToken).ConfigureAwait(false);
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

    internal async Task<List<AttributeConfiguration>> GetAttributeConfigurationsForEntityConfiguration(
        EntityConfiguration entityConfiguration, CancellationToken cancellationToken = default
    )
    {
        var attributeConfigurations = new List<AttributeConfiguration>();

        foreach (EntityConfigurationAttributeReference attributeReference in entityConfiguration.Attributes)
        {
            attributeConfigurations.Add(
                await AttributeConfigurationRepository.LoadAsyncOrThrowNotFound(
                    attributeReference.AttributeConfigurationId,
                    attributeReference.AttributeConfigurationId.ToString(),
                    cancellationToken
                )
            );
        }

        return attributeConfigurations;
    }
    #endregion

    #region EntityInstance

    public async Task<(EntityInstanceViewModel?, ProblemDetails?)> CreateEntityInstance(
         EntityInstanceCreateRequest entity,
         bool dryRun = false,
         bool requiredAttributesCanBeNull = false,
         CancellationToken cancellationToken = default
    )
    {
        EntityConfiguration? entityConfiguration = await EntityConfigurationRepository.LoadAsync(
            entity.EntityConfigurationId,
            entity.EntityConfigurationId.ToString(),
            cancellationToken
        );

        if (entityConfiguration == null)
        {
            return (null, new ValidationErrorResponse("EntityConfigurationId", "Configuration not found"))!;
        }

        List<AttributeConfiguration> attributeConfigurations =
            await GetAttributeConfigurationsForEntityConfiguration(
                entityConfiguration,
                cancellationToken
            );

        //TODO: add check for categoryPath
        var entityInstance = new EntityInstance(
            Guid.NewGuid(),
            entity.EntityConfigurationId,
            _mapper.Map<List<AttributeInstance>>(entity.Attributes),
            entity.MachineName,
            entity.TenantId,
            _mapper.Map<List<CategoryPath>>(entity.CategoryPaths)
        );

        var validationErrors = new Dictionary<string, string[]>();
        List<IGeneratedValueInfo?> generatedValues = new();

        foreach (AttributeConfiguration a in attributeConfigurations)
        {
            AttributeInstance? attributeValue = entityInstance.Attributes
                .FirstOrDefault(attr => a.MachineName == attr.ConfigurationAttributeMachineName);

            List<string> attrValidationErrors = a.ValidateInstance(attributeValue, requiredAttributesCanBeNull);
            if (attrValidationErrors is { Count: > 0 })
            {
                validationErrors.Add(a.MachineName, attrValidationErrors.ToArray());
            }

            generatedValues.Add(await _valueAttributeService.GenerateAttributeInstanceValue(entityConfiguration, a, attributeValue));
        }

        if (validationErrors.Count > 0)
        {
            return (null, new ValidationErrorResponse(validationErrors))!;
        }

        if (!dryRun)
        {
            var response = await _valueAttributeService.SaveValues(entityConfiguration.Id, generatedValues);

            foreach (var actionResponse in response.Where(x => x.Status == GeneratedValueActionStatus.Failed))
            {
                var attributeMachineName = attributeConfigurations.First(x => x.Id == actionResponse.AttributeConfigurationId).MachineName;

                validationErrors.Add(
                    attributeMachineName,
                    new string[] { $"Failed to generate value: {actionResponse.GeneratedValueType?.Name}" });
            }

            ProjectionDocumentSchema schema = ProjectionDocumentSchemaFactory
                .FromEntityConfiguration(entityConfiguration, attributeConfigurations);

            IProjectionRepository projectionRepository = _projectionRepositoryFactory.GetProjectionRepository(schema);
            await projectionRepository.EnsureIndex(cancellationToken).ConfigureAwait(false);

            var entityInstanceSaved =
                await EntityInstanceRepository.SaveAsync(_userInfo, entityInstance, cancellationToken);

            if (!entityInstanceSaved)
            {
                //TODO: What do we want to do with internal exceptions and unsuccessful flow?
                throw new Exception("Entity was not saved");
            }

            return (_mapper.Map<EntityInstanceViewModel>(entityInstance), null);
        }

        return (_mapper.Map<EntityInstanceViewModel>(entityInstance), null);
    }

    public async Task<EntityInstanceViewModel?> GetEntityInstance(Guid id, string partitionKey)
    {
        EntityInstance? entityInstance = await EntityInstanceRepository.LoadAsync(id, partitionKey);

        return _mapper.Map<EntityInstanceViewModel?>(entityInstance);
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
            await EntityInstanceRepository.LoadAsync(updateRequest.Id, partitionKey, cancellationToken);

        if (entityInstance == null)
        {
            return (null, new ValidationErrorResponse(nameof(updateRequest.Id), "Entity instance not found"))!;
        }

        EntityConfiguration? entityConfiguration = await EntityConfigurationRepository.LoadAsync(
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
        List<IGeneratedValueInfo?> generatedValues = new();

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
                        (IGeneratedValueInfo? valueInfo, List<string>? valueErrors) =
                            await _valueAttributeService.UpdateGeneratedValueDuringInstanceUpdate(entityConfiguration, attrConfig, newAttribute);

                        if (valueErrors == null)
                        {
                            entityInstance.UpdateAttributeInstance(newAttribute);

                            generatedValues.Add(valueInfo);
                        }
                        else
                        {
                            validationErrors.Add(newAttribute.ConfigurationAttributeMachineName, valueErrors.ToArray());
                        }
                    }
                }
                else
                {
                    generatedValues.Add(await _valueAttributeService.GenerateAttributeInstanceValue(entityConfiguration, attrConfig, newAttribute));

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
            await _valueAttributeService.SaveValues(entityConfiguration.Id, generatedValues);

            var entityInstanceSaved = await EntityInstanceRepository
                .SaveAsync(_userInfo, entityInstance, cancellationToken)
                .ConfigureAwait(false);
            if (!entityInstanceSaved)
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
        EntityConfiguration entityConfiguration = await EntityConfigurationRepository.LoadAsyncOrThrowNotFound(
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

    #endregion
}

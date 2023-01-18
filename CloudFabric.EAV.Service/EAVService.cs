using System.Globalization;
using System.Text.RegularExpressions;

using AutoMapper;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Models.ViewModels.Attributes;
using CloudFabric.EAV.Models.ViewModels.EAV;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Persistence;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CloudFabric.EAV.Service;

public class EAVService : IEAVService
{
    private readonly ILogger<EAVService> _logger;
    private readonly IMapper _mapper;

    private readonly AggregateRepository<AttributeConfiguration> _attributeConfigurationRepository;
    private readonly AggregateRepository<EntityConfiguration> _entityConfigurationRepository;
    private readonly AggregateRepository<EntityInstance> _entityInstanceRepository;

    private readonly IProjectionRepository<AttributeConfigurationProjectionDocument>
        _attributeConfigurationProjectionRepository;

    private readonly IProjectionRepository<EntityConfigurationProjectionDocument>
        _entityConfigurationProjectionRepository;

    private readonly AggregateRepositoryFactory _aggregateRepositoryFactory;
    private readonly ProjectionRepositoryFactory _projectionRepositoryFactory;

    private readonly EventUserInfo _userInfo;

    private readonly EntityInstanceFromDictionaryDeserializer _entityInstanceFromDictionaryDeserializer;

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

        _attributeConfigurationProjectionRepository = _projectionRepositoryFactory
            .GetProjectionRepository<AttributeConfigurationProjectionDocument>();
        _entityConfigurationProjectionRepository = _projectionRepositoryFactory
            .GetProjectionRepository<EntityConfigurationProjectionDocument>();

        _entityInstanceFromDictionaryDeserializer = new EntityInstanceFromDictionaryDeserializer(_mapper);
    }

    #region EntityConfiguration

    public async Task<EntityConfigurationViewModel> GetEntityConfiguration(Guid id, string partitionKey)
    {
        var entityConfiguration = await _entityConfigurationRepository.LoadAsync(id, partitionKey);

        return _mapper.Map<EntityConfigurationViewModel>(entityConfiguration);
    }
    //
    // public async Task<EntityConfigurationWithAttributesViewModel> GetEntityConfigurationWithAttributes(
    //     Guid id,
    //     string partitionKey,
    //     CancellationToken cancellationToken = default(CancellationToken)
    // )
    // {
    //     var entityConfiguration = await _entityConfigurationRepository.LoadAsyncOrThrowNotFound(
    //         id,
    //         partitionKey,
    //         cancellationToken
    //     );
    //
    //     var entityConfigurationViewModel = _mapper.Map<EntityConfigurationWithAttributesViewModel>(entityConfiguration);
    //
    //     var attributes = await GetAttributeConfigurationsForEntityConfiguration(
    //         entityConfiguration,
    //         cancellationToken
    //     );
    //
    //     entityConfigurationViewModel.Attributes = _mapper.Map<List<AttributeConfigurationViewModel>>(attributes);
    //
    //     return entityConfigurationViewModel;
    // }

    public async Task<ProjectionQueryResult<AttributeConfigurationListItemViewModel>> ListAttributes(
        ProjectionQuery query,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    )
    {
        var list = await _attributeConfigurationProjectionRepository.Query(query, partitionKey, cancellationToken);
        return _mapper.Map<ProjectionQueryResult<AttributeConfigurationListItemViewModel>>(list);
    }

    public async Task<ProjectionQueryResult<EntityConfigurationViewModel>> ListEntityConfigurations(
        ProjectionQuery query,
        string? partitionKey = null,
        CancellationToken cancellationToken = default
    )
    {
        var records = await _entityConfigurationProjectionRepository.Query(query, partitionKey, cancellationToken);
        return _mapper.Map<ProjectionQueryResult<EntityConfigurationViewModel>>(records);
    }

    public async Task<(AttributeConfigurationViewModel?, ValidationErrorResponse?)> CreateAttribute(
        AttributeConfigurationCreateUpdateRequest attributeConfigurationCreateUpdateRequest,
        CancellationToken cancellationToken = default
    )
    {
        EnsureAttributeMachineNameIsAdded(attributeConfigurationCreateUpdateRequest);

        var attribute = _mapper.Map<AttributeConfiguration>(attributeConfigurationCreateUpdateRequest);
        var validationErrors = attribute.Validate();
        if (validationErrors.Any())
        {
            return (null, new ValidationErrorResponse(attribute.MachineName, validationErrors.ToArray()));
        }
        
        await _attributeConfigurationRepository.SaveAsync(_userInfo, attribute, cancellationToken);

        return (_mapper.Map<AttributeConfigurationViewModel>(attribute), null);
    }

    public async Task<AttributeConfigurationViewModel> GetAttribute(Guid id, string partitionKey, CancellationToken cancellationToken = default)
    {
        var attribute = await _attributeConfigurationRepository.LoadAsyncOrThrowNotFound(id, partitionKey, CancellationToken.None);

        if (attribute.IsDeleted)
        {
            throw new NotFoundException("Attribute not found");
        }

        return _mapper.Map<AttributeConfigurationViewModel>(attribute);
    }

    public async Task<(AttributeConfigurationViewModel?, ProblemDetails?)> UpdateAttribute(Guid id, AttributeConfigurationCreateUpdateRequest updateRequest, CancellationToken cancellationToken = default)
    {
        AttributeConfiguration? attribute = await _attributeConfigurationRepository.LoadAsync(id, id.ToString(), CancellationToken.None);

        if (attribute == null || attribute.IsDeleted)
        {
            return (null, new ValidationErrorResponse(nameof(id), "Attribute not found"));
        }

        if (attribute.ValueType != updateRequest.ValueType)
        {
            return (null, new ValidationErrorResponse(nameof(updateRequest.ValueType), "Attribute type cannot be changed"));
        }

        updateRequest.MachineName = attribute.MachineName;
        AttributeConfiguration updatedAttribute = _mapper.Map<AttributeConfiguration>(updateRequest);

        var validationErrors = updatedAttribute.Validate();
        if (validationErrors.Any())
        {
            return (null, new ValidationErrorResponse(updatedAttribute.MachineName, validationErrors.ToArray()));
        }
        attribute.UpdateAttribute(updatedAttribute);
        await _attributeConfigurationRepository.SaveAsync(_userInfo, attribute, cancellationToken);

        return (_mapper.Map<AttributeConfigurationViewModel>(attribute), null);
    }

    public async Task<(EntityConfigurationViewModel?, ProblemDetails?)> CreateEntityConfiguration(
        EntityConfigurationCreateRequest entityConfigurationCreateRequest,
        CancellationToken cancellationToken
    )
    {
        foreach (var attribute in entityConfigurationCreateRequest.Attributes.Where(x => x is AttributeConfigurationCreateUpdateRequest))
        {
            EnsureAttributeMachineNameIsAdded(
                (AttributeConfigurationCreateUpdateRequest)attribute
            );
        }

        if (!await CheckAttributesListMachineNameUnique(entityConfigurationCreateRequest.Attributes, cancellationToken))
        {
            return (
                null,
                new ValidationErrorResponse(nameof(entityConfigurationCreateRequest.Attributes), "Attributes machine name must be unique")
            )!;
        }

        foreach (var attribute in entityConfigurationCreateRequest.Attributes.Where(x => x is EntityAttributeConfigurationCreateUpdateReferenceRequest))
        {
            var requestAttribute = (EntityAttributeConfigurationCreateUpdateReferenceRequest)attribute;

            var attributeConfiguration = await _attributeConfigurationRepository.LoadAsyncOrThrowNotFound(
                    requestAttribute.AttributeConfigurationId,
                    requestAttribute.AttributeConfigurationId.ToString(),
                    cancellationToken
                    );

            if (attributeConfiguration.IsDeleted)
            {
                return (
                    null,
                    new ValidationErrorResponse(nameof(entityConfigurationCreateRequest.Attributes), "One or more attribute not found")
                )!;
            }
        }

        var allAttrProblemDetails = new List<ValidationErrorResponse>();
        for (var i = 0; i < entityConfigurationCreateRequest.Attributes.Count; i++)
        {
            var attribute = entityConfigurationCreateRequest.Attributes[i];

            if (!(attribute is EntityAttributeConfigurationCreateUpdateReferenceRequest))
            {
                var (attrCreated, attrProblemDetails)  = await CreateAttribute(
                    (AttributeConfigurationCreateUpdateRequest)attribute,
                    cancellationToken
                );

                if (attrProblemDetails != null)
                {
                    allAttrProblemDetails.Add(attrProblemDetails);
                }
                else
                {
                    entityConfigurationCreateRequest.Attributes[i] =
                        new EntityAttributeConfigurationCreateUpdateReferenceRequest()
                        {
                            AttributeConfigurationId = attrCreated.Id
                        };    
                }
            }
        }

        if (allAttrProblemDetails.Any())
        {
            var allErrors = allAttrProblemDetails.SelectMany(pd => pd.Errors)
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
            entityConfigurationCreateRequest.TenantId,
            entityConfigurationCreateRequest.Metadata
        );

        var entityValidationErrors = entityConfiguration.Validate();
        if (entityValidationErrors.Any())
        {
            return (null, new ValidationErrorResponse(entityConfiguration.MachineName, entityValidationErrors.ToArray()));
        }
        await _entityConfigurationRepository.SaveAsync(
            _userInfo,
            entityConfiguration,
            cancellationToken
        );

        return (_mapper.Map<EntityConfigurationViewModel>(entityConfiguration), null)!;
    }

    public async Task<(EntityConfigurationViewModel?, ProblemDetails?)> UpdateEntityConfiguration(
        EntityConfigurationUpdateRequest entityUpdateRequest,
        CancellationToken cancellationToken)
    {
        foreach (var attribute in entityUpdateRequest.Attributes.Where(x => x is AttributeConfigurationCreateUpdateRequest))
        {
            EnsureAttributeMachineNameIsAdded(
                (AttributeConfigurationCreateUpdateRequest)attribute
            );
        }

        if (!await CheckAttributesListMachineNameUnique(entityUpdateRequest.Attributes, cancellationToken))
        {
            return (
                null,
                new ValidationErrorResponse(nameof(entityUpdateRequest.Attributes), "Attributes machine name must be unique")
            )!;
        }

        var entityConfiguration = await _entityConfigurationRepository.LoadAsync(
            entityUpdateRequest.Id,
            entityUpdateRequest.Id.ToString(),
            cancellationToken
        );

        if (entityConfiguration == null)
        {
            return (null, new ValidationErrorResponse(nameof(entityUpdateRequest.Id), "Entity configuration not found"))!;
        }

        // Update config name
        foreach (var name in entityUpdateRequest.Name.Where(name => !entityConfiguration.Name.Any(
                     x => x.CultureInfoId == name.CultureInfoId && x.String == name.String)))
        {
            entityConfiguration.UpdateName(name.String, name.CultureInfoId);
        }

        List<Guid> reservedAttributes = new List<Guid>();
        foreach (var attributeUpdate in entityUpdateRequest.Attributes)
        {
            if (attributeUpdate is EntityAttributeConfigurationCreateUpdateReferenceRequest attributeReferenceUpdate)
            {
                // for references we need to just add/remove the reference
                var attributeShouldBeAdded = entityConfiguration.Attributes
                    .All(a => a.AttributeConfigurationId != attributeReferenceUpdate.AttributeConfigurationId);

                var attributeConfiguration = await _attributeConfigurationRepository.LoadAsync(
                    attributeReferenceUpdate.AttributeConfigurationId,
                    attributeReferenceUpdate.AttributeConfigurationId.ToString(),
                    cancellationToken
                    );

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
                var (attributeCreated, attrProblemDetails) = await CreateAttribute(
                    attributeCreateRequest,
                    cancellationToken
                );
                if (attrProblemDetails != null)
                {
                    
                }
                entityConfiguration.AddAttribute(attributeCreated.Id);
                reservedAttributes.Add(attributeCreated.Id);
            }
        }

        var attributesToRemove = entityConfiguration.Attributes.ExceptBy(
            reservedAttributes,
            x => x.AttributeConfigurationId
        );

        foreach (var attribute in attributesToRemove)
        {
            entityConfiguration.RemoveAttribute(attribute.AttributeConfigurationId);
        }

        entityConfiguration.UpdateMetadata(
            entityConfiguration.Metadata.ToDictionary(k => k.Key, k => k.Value)
        );

        var entityValidationErrors = entityConfiguration.Validate();
        if (entityValidationErrors.Any())
        {
            return (null, new ValidationErrorResponse(entityConfiguration.MachineName, entityValidationErrors.ToArray()));
        }

        await _entityConfigurationRepository.SaveAsync(_userInfo, entityConfiguration, cancellationToken);

        return (_mapper.Map<EntityConfigurationViewModel>(entityConfiguration), null)!;
    }

    public async Task<(EntityConfigurationViewModel?, ProblemDetails?)> AddAttributeToEntityConfiguration(
        Guid attributeId,
        Guid entityConfigurationId,
        CancellationToken cancellationToken = default
    )
    {
        var attributeConfiguration = await _attributeConfigurationRepository.LoadAsyncOrThrowNotFound(
            attributeId,
            attributeId.ToString(),
            cancellationToken
        );

        if (attributeConfiguration.IsDeleted)
        {
            return (null, new ValidationErrorResponse(nameof(attributeId), "Attribute not found"));
        }
        
        var entityConfiguration = await _entityConfigurationRepository.LoadAsyncOrThrowNotFound(
            entityConfigurationId,
            entityConfigurationId.ToString(),
            cancellationToken
        );

        if (entityConfiguration.Attributes.Any(x => x.AttributeConfigurationId == attributeConfiguration.Id))
        {
            return (null, new ValidationErrorResponse(nameof(attributeId), "Attribute has already been added"))!;
        }

        if (!await IsAttributeMachineNameUniqueForEntityConfiguration(
                attributeConfiguration.MachineName,
                entityConfiguration,
                cancellationToken
            )
        )
        {
            return (null, new ValidationErrorResponse(nameof(attributeId), "Attributes machine name must be unique"));
        }

        entityConfiguration.AddAttribute(attributeId);
        await _entityConfigurationRepository.SaveAsync(_userInfo, entityConfiguration, cancellationToken);

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
        );

        if (!await IsAttributeMachineNameUniqueForEntityConfiguration(
                attributeConfigurationCreateUpdateRequest.MachineName,
                entityConfiguration,
                cancellationToken
            )
        )
        {
            return (
                null,
                new ValidationErrorResponse(nameof(attributeConfigurationCreateUpdateRequest.MachineName), "Machine name already exists in this configuration. Please consider using different name")
            )!;
        }

        (AttributeConfigurationViewModel? createdAttribute, ValidationErrorResponse? attrProblemDetails) = await CreateAttribute(attributeConfigurationCreateUpdateRequest, cancellationToken);
        if (attrProblemDetails != null)
        {
            return (null, attrProblemDetails);
        }
        entityConfiguration.AddAttribute(createdAttribute.Id);
        await _entityConfigurationRepository.SaveAsync(_userInfo, entityConfiguration, cancellationToken);

        return (createdAttribute, null)!;
    }
    
    public async Task DeleteAttributesFromEntityConfiguration(List<Guid> attributesIds, Guid entityConfigurationId, CancellationToken cancellationToken = default)
    {
        var entityConfiguration = await _entityConfigurationRepository.LoadAsyncOrThrowNotFound(
            entityConfigurationId,
            entityConfigurationId.ToString(),
            cancellationToken
        );

        List<AttributeConfiguration> listAttributesConfigurations = await GetAttributeConfigurationsForEntityConfiguration(entityConfiguration, cancellationToken);

        foreach (var attributeId in attributesIds)
        {
            if (listAttributesConfigurations.Any(x => x.Id == attributeId))
            {
                entityConfiguration.RemoveAttribute(attributeId);
            }
        }

        await _entityConfigurationRepository.SaveAsync(_userInfo, entityConfiguration, cancellationToken);
    }

    public async Task DeleteAttributes(List<Guid> attributesIds, CancellationToken cancellationToken = default)
    {
        foreach (var attributeId in attributesIds)
        {
            var attributeConfiguration = await _attributeConfigurationRepository.LoadAsync(
            attributeId,
            attributeId.ToString(),
            cancellationToken
            );

            if (attributeConfiguration != null && !attributeConfiguration.IsDeleted)
            {
                attributeConfiguration.Delete();

                await _attributeConfigurationRepository.SaveAsync(_userInfo, attributeConfiguration, cancellationToken);
            }
        }

        string filterPropertyName = string.Concat(
            nameof(EntityConfigurationProjectionDocument.Attributes),
            ".",
            nameof(AttributeConfigurationReference.AttributeConfigurationId
        ));

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
                        propertyName: filterPropertyName,
                        oper: FilterOperator.Equal,
                        value: attributeId)
            ));
        }

        ProjectionQueryResult<EntityConfigurationProjectionDocument> entityConfigurations = await _entityConfigurationProjectionRepository.Query(
            new ProjectionQuery
            {
                Filters = new List<Filter>
                {
                    filters
                }
            },
            cancellationToken: cancellationToken
        );

        if (entityConfigurations.Records.Count > 0)
        {
            var entitiesIdsFromQuery = entityConfigurations.Records.Select(x => x.Document.Id);

            foreach (var entityId in entitiesIdsFromQuery)
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

    #region EntityInstance

    public async Task<(EntityInstanceViewModel, ProblemDetails)> CreateEntityInstance(
        EntityInstanceCreateRequest entity, CancellationToken cancellationToken = default
    ) {
        EntityConfiguration? entityConfiguration = await _entityConfigurationRepository.LoadAsync(
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

        var entityInstance = new EntityInstance(
            Guid.NewGuid(),
            entity.EntityConfigurationId,
            _mapper.Map<List<AttributeInstance>>(entity.Attributes),
            entity.TenantId
        );

        var validationErrors = new Dictionary<string, string[]>();
        foreach (var a in attributeConfigurations)
        {
            var attributeValue = entityInstance.Attributes
                .FirstOrDefault(attr => a.MachineName == attr.ConfigurationAttributeMachineName);

            var attrValidationErrors = a.ValidateInstance(attributeValue);
            if (attrValidationErrors is { Count: > 0 })
            {
                validationErrors.Add(a.MachineName, attrValidationErrors.ToArray());
            }
        }

        if (validationErrors.Count > 0)
        {
            return (null, new ValidationErrorResponse(validationErrors))!;
        }

        var saved = await _entityInstanceRepository.SaveAsync(_userInfo, entityInstance);
        if (!saved)
        {
            //TODO: What do we want to do with internal exceptions and unsuccessful flow?
            throw new Exception("Entity was not saved");
        }

        return (_mapper.Map<EntityInstanceViewModel>(entityInstance), null)!;
    }

    public async Task<EntityInstanceViewModel> GetEntityInstance(Guid id, string partitionKey)
    {
        var entityInstance = await _entityInstanceRepository.LoadAsync(id, partitionKey);

        return _mapper.Map<EntityInstanceViewModel>(entityInstance);
    }

    public async Task<(EntityInstanceViewModel, ProblemDetails)> UpdateEntityInstance(string partitionKey, EntityInstanceUpdateRequest updateRequest, CancellationToken cancellationToken)
    {
        EntityInstance? entityInstance = await _entityInstanceRepository.LoadAsync(updateRequest.Id, partitionKey, cancellationToken);

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
            return (null, new ValidationErrorResponse(nameof(updateRequest.EntityConfigurationId), "Entity configuration not found"))!;
        }

        var entityConfigurationAttributeConfigurations = await GetAttributeConfigurationsForEntityConfiguration(
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

            AttributeInstance? newAttribute = _mapper.Map<AttributeInstance>(newAttributeRequest);
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
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var entityConfiguration = await _entityConfigurationRepository.LoadAsyncOrThrowNotFound(
            entityConfigurationId,
            entityConfigurationId.ToString(),
            cancellationToken
        );

        var attributes = await GetAttributeConfigurationsForEntityConfiguration(
            entityConfiguration,
            cancellationToken
        );

        var schema = CloudFabric.EAV.Domain.Projections.EntityInstanceProjection.ProjectionDocumentSchemaFactory
            .FromEntityConfiguration(entityConfiguration, attributes);

        var projectionRepository = _projectionRepositoryFactory.GetProjectionRepository(schema);

        var results = await projectionRepository.Query(query, entityConfigurationId.ToString(), cancellationToken);

        return results.TransformResultDocuments(
            r => _entityInstanceFromDictionaryDeserializer.Deserialize(entityConfiguration, attributes, r)
        );
    }

    private async Task<List<AttributeConfiguration>> GetAttributeConfigurationsForEntityConfiguration(
        EntityConfiguration entityConfiguration, CancellationToken cancellationToken = default
    )
    {
        List<AttributeConfiguration> attributeConfigurations = new List<AttributeConfiguration>();

        foreach (var attributeReference in entityConfiguration.Attributes)
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
            Regex specSymbolsRegex = new Regex("[^\\d\\w_]*");
            attributeRequest.MachineName = specSymbolsRegex.Replace(machineName, "").ToLower();
        }
    }

    private async Task<bool> IsAttributeMachineNameUniqueForEntityConfiguration(string machineName, EntityConfiguration entityConfiguration, CancellationToken cancellationToken)
    {
        List<Guid> attributesIds = entityConfiguration.Attributes
            .Select(x => x.AttributeConfigurationId)
            .ToList();

        if (!attributesIds.Any())
        {
            return true;
        }

        // create attributes filter
        var attributes = await GetAttributesByIds(attributesIds, cancellationToken);

        if (attributes.Records.Any(x => x.Document?.MachineName == machineName))
        {
            return false;
        }

        return true;
    }

    private async Task<bool> CheckAttributesListMachineNameUnique(List<EntityAttributeConfigurationCreateUpdateRequest> attributesRequest, CancellationToken cancellationToken)
    {
        // validate reference attributes don't have the same machine name
        var referenceAttributes = attributesRequest
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
        var newAttributes = attributesRequest.Where(x => x is AttributeConfigurationCreateUpdateRequest);

        foreach (var attribute in newAttributes)
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

    private async Task<ProjectionQueryResult<AttributeConfigurationListItemViewModel>> GetAttributesByIds(List<Guid> attributesIds, CancellationToken cancellationToken)
    {
        // create attributes filter
        Filter attributeIdFilter = new(nameof(AttributeConfigurationProjectionDocument.Id), FilterOperator.Equal, attributesIds[0]);

        foreach (Guid attributesId in attributesIds.Skip(1))
        {
            attributeIdFilter.Filters.Add(
                new FilterConnector(
                    FilterLogic.Or, new Filter(nameof(AttributeConfigurationProjectionDocument.Id), FilterOperator.Equal, attributesId))
            );
        }

        ProjectionQueryResult<AttributeConfigurationListItemViewModel> attributes = await ListAttributes(
            new ProjectionQuery
            {
                Filters = new List<Filter>
                {
                    attributeIdFilter
                }
            },
            cancellationToken: cancellationToken
        );

        return attributes;
    }
}

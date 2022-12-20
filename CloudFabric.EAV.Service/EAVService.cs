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
    private readonly IProjectionRepository<EntityConfigurationProjectionDocument> _entityConfigurationProjectionRepository;

    private readonly EventUserInfo _userInfo;

    public EAVService(
        ILogger<EAVService> logger,
        IMapper mapper,
        AggregateRepository<AttributeConfiguration> attributeConfigurationRepository,
        AggregateRepository<EntityConfiguration> entityConfigurationRepository,
        AggregateRepository<EntityInstance> entityInstanceRepository,
        IProjectionRepository<AttributeConfigurationProjectionDocument> attributeConfigurationProjectionRepository,
        IProjectionRepository<EntityConfigurationProjectionDocument> entityConfigurationProjectionRepository,
        EventUserInfo userInfo
    )
    {
        _logger = logger;
        _mapper = mapper;
        _attributeConfigurationRepository = attributeConfigurationRepository;
        _entityConfigurationRepository = entityConfigurationRepository;
        _entityInstanceRepository = entityInstanceRepository;
        _attributeConfigurationProjectionRepository = attributeConfigurationProjectionRepository;
        _entityConfigurationProjectionRepository = entityConfigurationProjectionRepository;
        _userInfo = userInfo;
    }

    #region EntityConfiguration

    public async Task<EntityConfigurationViewModel> GetEntityConfiguration(Guid id, string partitionKey)
    {
        var entityConfiguration = await _entityConfigurationRepository.LoadAsync(id, partitionKey);

        return _mapper.Map<EntityConfigurationViewModel>(entityConfiguration);
    }

    public async Task<List<AttributeConfigurationListItemViewModel>> ListAttributes(ProjectionQuery query, 
        string? partitionKey = null, 
        CancellationToken cancellationToken = default
    )
    {
        var list = await _attributeConfigurationProjectionRepository.Query(query, partitionKey, cancellationToken);
        return _mapper.Map<List<AttributeConfigurationListItemViewModel>>(list);
    }
    
    public async Task<List<EntityConfigurationViewModel>> ListEntityConfigurations(
        ProjectionQuery query, 
        string? partitionKey = null, 
        CancellationToken cancellationToken = default
    )
    {
        var records = await _entityConfigurationProjectionRepository.Query(query, partitionKey, cancellationToken);
        return _mapper.Map<List<EntityConfigurationViewModel>>(records);
    }

    public async Task<AttributeConfigurationViewModel> CreateAttribute(
        AttributeConfigurationCreateUpdateRequest attributeConfigurationCreateUpdateRequest,
        CancellationToken cancellationToken = default
    )
    {
        EnsureAttributeMachineNameIsAdded(attributeConfigurationCreateUpdateRequest);

        var attribute = _mapper.Map<AttributeConfiguration>(attributeConfigurationCreateUpdateRequest);

        await _attributeConfigurationRepository.SaveAsync(_userInfo, attribute, cancellationToken);

        return _mapper.Map<AttributeConfigurationViewModel>(attribute);
    }

    public async Task<AttributeConfigurationViewModel> GetAttribute(Guid id, string partitionKey, CancellationToken cancellationToken = default)
    {
        var attribute = await _attributeConfigurationRepository.LoadAsyncOrThrowNotFound(id, partitionKey, CancellationToken.None);
        return _mapper.Map<AttributeConfigurationViewModel>(attribute);
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

        for (var i = 0; i < entityConfigurationCreateRequest.Attributes.Count; i++)
        {
            var attribute = entityConfigurationCreateRequest.Attributes[i];

            if (!(attribute is EntityAttributeConfigurationCreateUpdateReferenceRequest))
            {
                var attrCreated = await CreateAttribute(
                    (AttributeConfigurationCreateUpdateRequest)attribute,
                    cancellationToken
                );

                entityConfigurationCreateRequest.Attributes[i] =
                    new EntityAttributeConfigurationCreateUpdateReferenceRequest()
                    {
                        AttributeConfigurationId = attrCreated.Id
                    };
            }
        }

        var entityConfiguration = new EntityConfiguration(
            Guid.NewGuid(),
            _mapper.Map<List<LocalizedString>>(entityConfigurationCreateRequest.Name),
            entityConfigurationCreateRequest.MachineName,
            _mapper.Map<List<EntityConfigurationAttributeReference>>(entityConfigurationCreateRequest.Attributes),
            entityConfigurationCreateRequest.TenantId,
            entityConfigurationCreateRequest.Metadata
        );

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
            throw new NotFoundException();
        }

        // Update config name
        foreach (var name in entityUpdateRequest.Name.Where(name => !entityConfiguration.Name.Any(
                     x => x.CultureInfoId == name.CultureInfoId && x.String == name.String)))
        {
            entityConfiguration.UpdateName(name.String, name.CultureInfoId);
        }

        List<Guid> addedAttributes = new();
        foreach (var attributeUpdate in entityUpdateRequest.Attributes)
        {
            if (attributeUpdate is EntityAttributeConfigurationCreateUpdateReferenceRequest attributeReferenceUpdate)
            {
                // for references we need to just add/remove the reference
                var attributeShouldBeAdded = entityConfiguration.Attributes
                    .All(a => a.AttributeConfigurationId != attributeReferenceUpdate.AttributeConfigurationId); 
                if (attributeShouldBeAdded)
                {
                    entityConfiguration.AddAttribute(attributeReferenceUpdate.AttributeConfigurationId);
                }
            }
            else if (attributeUpdate is AttributeConfigurationCreateUpdateRequest attributeCreateRequest)
            {
                var attributeCreated = await CreateAttribute(
                    attributeCreateRequest,
                    cancellationToken
                );

                entityConfiguration.AddAttribute(attributeCreated.Id);
                addedAttributes.Add(attributeCreated.Id);
            }
        }

        var attributesToRemove = entityConfiguration.Attributes.ExceptBy(
            entityUpdateRequest.Attributes
                .Where(a => a is EntityAttributeConfigurationCreateUpdateReferenceRequest)
                .Select(a => ((a as EntityAttributeConfigurationCreateUpdateReferenceRequest)!).AttributeConfigurationId)
                .Concat(addedAttributes),
            x => x.AttributeConfigurationId
        );

        foreach (var attribute in attributesToRemove)
        {
            entityConfiguration.RemoveAttribute(attribute.AttributeConfigurationId);
        }
        
        entityConfiguration.UpdateMetadata(
            entityConfiguration.Metadata.ToDictionary(k => k.Key, k => k.Value)
        );
        
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

        AttributeConfigurationViewModel createdAttribute = await CreateAttribute(attributeConfigurationCreateUpdateRequest, cancellationToken);
        
        entityConfiguration.AddAttribute(createdAttribute.Id);
        await _entityConfigurationRepository.SaveAsync(_userInfo, entityConfiguration, cancellationToken);

        return (createdAttribute, null)!;
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

            var attrValidationErrors = a.Validate(attributeValue);
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
            throw new NotFoundException("Entity Instance was not found");
        }
        
        EntityConfiguration? entityConfiguration = await _entityConfigurationRepository.LoadAsync(
            entityInstance.EntityConfigurationId,
            entityInstance.EntityConfigurationId.ToString(),
            cancellationToken
        );
        
        if (entityConfiguration == null)
        {
            throw new NotFoundException("Entity Configuration was not found");
        }

        var entityConfigurationAttributeConfigurations =
           await GetAttributeConfigurationsForEntityConfiguration(entityConfiguration);
        
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
            List<string> errors = attrConfiguration.Validate(null);
            
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
            List<string> errors = attrConfig.Validate(newAttribute);
        
            if (errors.Count == 0)
            {
                AttributeInstance? currentAttribute = entityInstance.Attributes.FirstOrDefault(x => x.ConfigurationAttributeMachineName == newAttributeRequest.ConfigurationAttributeMachineName);
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

    private async Task<List<AttributeConfiguration>> GetAttributeConfigurationsForEntityConfiguration(
        EntityConfiguration entityConfiguration, CancellationToken cancellationToken = default
    ) {
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

        if (attributes.Any(x => x.MachineName == machineName))
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
                .Select(x => x.MachineName)
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

    private async Task<List<AttributeConfigurationListItemViewModel>> GetAttributesByIds(List<Guid> attributesIds, CancellationToken cancellationToken)
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
        
        List<AttributeConfigurationListItemViewModel> attributes = await ListAttributes(
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
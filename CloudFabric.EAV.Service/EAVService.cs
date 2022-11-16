using AutoMapper;

using CloudFabric.EAV.Domain.Events.Configuration.Attribute;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.EAV.Models.RequestModels;
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

    private readonly EventUserInfo _userInfo;

    public EAVService(
        ILogger<EAVService> logger,
        IMapper mapper,
        AggregateRepository<AttributeConfiguration> attributeConfigurationRepository,
        AggregateRepository<EntityConfiguration> entityConfigurationRepository,
        AggregateRepository<EntityInstance> entityInstanceRepository,
        IProjectionRepository<AttributeConfigurationProjectionDocument> attributeConfigurationProjectionRepository,
        EventUserInfo userInfo
    )
    {
        _logger = logger;
        _mapper = mapper;
        _attributeConfigurationRepository = attributeConfigurationRepository;
        _entityConfigurationRepository = entityConfigurationRepository;
        _entityInstanceRepository = entityInstanceRepository;
        _attributeConfigurationProjectionRepository = attributeConfigurationProjectionRepository;
        _userInfo = userInfo;
    }

    #region EntityConfiguration

    public async Task<EntityConfigurationViewModel> GetEntityConfiguration(Guid id, string partitionKey)
    {
        var entityConfiguration = await _entityConfigurationRepository.LoadAsync(id, partitionKey);

        return _mapper.Map<EntityConfigurationViewModel>(entityConfiguration);
    }

    public async Task<List<AttributeConfigurationListItemViewModel>> ListAttributes(int take, int skip = 0)
    {
        var list = await _attributeConfigurationProjectionRepository.Query(new ProjectionQuery()
        {
            Limit = take,
            Offset = 0
        });
        
        return _mapper.Map<List<AttributeConfigurationListItemViewModel>>(list);
    }
    
    // public async Task<List<EntityConfigurationViewModel>> ListEntityConfigurations(int take, int skip = 0)
    // {
    //     return _mapper.Map<List<EntityConfigurationViewModel>>(records);
    // }

    public async Task<AttributeConfigurationViewModel> CreateAttribute(
        AttributeConfigurationCreateUpdateRequest attributeConfigurationCreateUpdateRequest,
        CancellationToken cancellationToken = default
    )
    {
        var attribute = _mapper.Map<AttributeConfiguration>(attributeConfigurationCreateUpdateRequest);

        await _attributeConfigurationRepository.SaveAsync(_userInfo, attribute, cancellationToken);

        return _mapper.Map<AttributeConfigurationViewModel>(attribute);
    }

    public async Task<EntityConfigurationViewModel> CreateEntityConfiguration(
        EntityConfigurationCreateRequest entityConfigurationCreateRequest, CancellationToken cancellationToken
    )
    {
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
            _mapper.Map<List<EntityConfigurationAttributeReference>>(entityConfigurationCreateRequest.Attributes)
        );

        var created = await _entityConfigurationRepository.SaveAsync(
            _userInfo,
            entityConfiguration,
            cancellationToken
        );

        return _mapper.Map<EntityConfigurationViewModel>(entityConfiguration);
    }

    public async Task<EntityConfigurationViewModel> UpdateEntityConfiguration(
        EntityConfigurationUpdateRequest entityUpdateRequest,
        CancellationToken cancellationToken)
    {
        var entityConfiguration = await _entityConfigurationRepository.LoadAsync(
            entityUpdateRequest.Id,
            entityUpdateRequest.Id.ToString(),
            cancellationToken
        );
        
        if (entityConfiguration == null)
        {
            throw new NotFoundException();
        }

        foreach (var name in entityUpdateRequest.Name.Where(name => !entityConfiguration.Name.Any(
                     x => x.CultureInfoId == name.CultureInfoId && x.String == name.String)))
        {
            entityConfiguration.UpdateName(name.String, name.CultureInfoId);
        }

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
            }
        }

        var attributesToRemove = entityConfiguration.Attributes.ExceptBy(
            entityUpdateRequest.Attributes
                .Where(a => a is EntityAttributeConfigurationCreateUpdateReferenceRequest)
                .Select(a => ((a as EntityAttributeConfigurationCreateUpdateReferenceRequest)!).AttributeConfigurationId),
            x => x.AttributeConfigurationId
        );

        foreach (var attribute in attributesToRemove)
        {
            entityConfiguration.RemoveAttribute(attribute.AttributeConfigurationId);
        }
        
        await _entityConfigurationRepository.SaveAsync(_userInfo, entityConfiguration, cancellationToken);

        return _mapper.Map<EntityConfigurationViewModel>(entityConfiguration);
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
        var entityInstance = new EntityInstance(
            Guid.NewGuid(),
            entity.EntityConfigurationId,
            _mapper.Map<List<AttributeInstance>>(entity.Attributes)
        );

        var entityConfiguration = await _entityConfigurationRepository.LoadAsync(
            entityInstance.EntityConfigurationId,
            entityInstance.EntityConfigurationId.ToString()
        );
        
        if (entityConfiguration == null)
        {
            return (null, new ValidationErrorResponse("EntityConfigurationId", "Configuration not found"))!;
        }

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

        return (_mapper.Map<EntityInstanceViewModel>(entityInstance), null);
    }

    public async Task<EntityInstanceViewModel> GetEntityInstance(Guid id, string partitionKey)
    {
        var entityInstance = await _entityInstanceRepository.LoadAsync(id, partitionKey);

        return _mapper.Map<EntityInstanceViewModel>(entityInstance);
    }

    public Task UpdateEntityInstance(Guid userId, EntityInstanceUpdateRequest entity)
    {
        throw new NotImplementedException();
    }

    #endregion
}
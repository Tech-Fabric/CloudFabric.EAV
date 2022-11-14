using AutoMapper;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Models.ViewModels.EAV;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;
using CloudFabric.EventSourcing.EventStore.Persistence;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CloudFabric.EAV.Service;

public class EAVService : IEAVService
{
    private readonly AggregateRepository<EntityConfiguration> _entityConfigurationRepository;
    private readonly AggregateRepository<EntityInstance> _entityInstanceRepository;
    private readonly ILogger<EAVService> _logger;
    private readonly IMapper _mapper;

    private readonly EventUserInfo _userInfo;

    public EAVService(
        ILogger<EAVService> logger,
        IMapper mapper,
        AggregateRepository<EntityConfiguration> entityConfigurationRepository,
        AggregateRepository<EntityInstance> entityInstanceRepository,
        EventUserInfo userInfo
    )
    {
        _logger = logger;
        _mapper = mapper;
        _entityConfigurationRepository = entityConfigurationRepository;
        _entityInstanceRepository = entityInstanceRepository;
        _userInfo = userInfo;
    }

    #region EntityConfiguration

    public async Task<EntityConfigurationViewModel> GetEntityConfiguration(Guid id, string partitionKey)
    {
        var entityConfiguration = await _entityConfigurationRepository.LoadAsync(id.ToString(), partitionKey);

        return _mapper.Map<EntityConfigurationViewModel>(entityConfiguration);
    }

    //public async Task<List<EntityConfigurationViewModel>> ListEntityConfigurations(int take, int skip = 0)
    //{


    //    return _mapper.Map<List<EntityConfigurationViewModel>>(records);
    //}

    public async Task<EntityConfigurationViewModel> CreateEntityConfiguration(
        EntityConfigurationCreateRequest entityConfigurationCreateRequest,
        CancellationToken cancellationToken
    )
    {
        var entityConfiguration = new EntityConfiguration(
            Guid.NewGuid(),
            _mapper.Map<List<LocalizedString>>(entityConfigurationCreateRequest.Name),
            entityConfigurationCreateRequest.MachineName,
            _mapper.Map<List<AttributeConfiguration>>(entityConfigurationCreateRequest.Attributes)
        );
        await _entityConfigurationRepository.SaveAsync(_userInfo, entityConfiguration, cancellationToken);

        return _mapper.Map<EntityConfigurationViewModel>(entityConfiguration);
    }

    public async Task<EntityConfigurationViewModel> UpdateEntityConfiguration(EntityConfigurationUpdateRequest entity, CancellationToken cancellationToken)
    {
        var entityConfiguration = await _entityConfigurationRepository.LoadAsync(entity.Id.ToString(), entity.PartitionKey, cancellationToken);

        if (entityConfiguration == null)
        {
            throw new NotFoundException();
        }

        // Update config name
        foreach (var name in entity.Name.Where(name => !entityConfiguration.Name.Any(x => x.CultureInfoId == name.CultureInfoId && x.String == name.String)))
        {
            entityConfiguration.UpdateName(name.String, name.CultureInfoId);
        }

        var attributesToRemove = entityConfiguration.Attributes
            .ExceptBy(
                entity.Attributes.Select(x => x.MachineName),
                x => x.MachineName
            );

        foreach (var attribute in attributesToRemove)
        {
            entityConfiguration.RemoveAttribute(attribute);
        }

        foreach (var newAttributeRequest in entity.Attributes)
        {
            var currentAttribute = entityConfiguration.Attributes.FirstOrDefault(x => x.MachineName == newAttributeRequest.MachineName);
            if (currentAttribute != null)
            {
                var newAttribute = _mapper.Map<AttributeConfiguration>(newAttributeRequest);
                if (!newAttribute.Equals(currentAttribute))
                {
                    entityConfiguration.UpdateAttribute(
                        _mapper.Map<AttributeConfiguration>(newAttributeRequest));
                }
            }
            else
            {
                entityConfiguration.AddAttribute(
                    _mapper.Map<AttributeConfiguration>(newAttributeRequest)
                );
            }
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

    public async Task<(EntityInstanceViewModel, ProblemDetails)> CreateEntityInstance(EntityInstanceCreateRequest entity)
    {
        var entityInstance = new EntityInstance(
            Guid.NewGuid(),
            entity.EntityConfigurationId,
            _mapper.Map<List<AttributeInstance>>(entity.Attributes)
        );

        //var entityConfiguration = await GetEntityConfiguration(entityInstance.EntityConfigurationId, EntityConfiguration.ENTITY_CONFIGURATION_PARTITION_KEY);

        var entityConfiguration = await _entityConfigurationRepository.LoadAsync(
            entityInstance.EntityConfigurationId.ToString(),
            entityInstance.EntityConfigurationId.ToString()
        );
        if (entityConfiguration == null)
        {
            return (null, new ValidationErrorResponse("EntityConfigurationId", "Configuration not found"))!;
        }
        var validationErrors = new Dictionary<string, string[]>();
        foreach (var a in entityConfiguration.Attributes)
        {
            var attributeValue = entityInstance.Attributes.FirstOrDefault(attr => a.MachineName == attr.ConfigurationAttributeMachineName);
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
        var entityInstance = await _entityInstanceRepository.LoadAsync(id.ToString(), partitionKey);

        return _mapper.Map<EntityInstanceViewModel>(entityInstance);
    }

    public async Task<(EntityInstanceViewModel, ProblemDetails)> UpdateEntityInstance(string partitionKey, EntityInstanceUpdateRequest updateRequest, CancellationToken cancellationToken)
    {

        EntityInstance? entityInstance = await _entityInstanceRepository.LoadAsync(updateRequest.Id.ToString(), partitionKey, cancellationToken);
        if (entityInstance == null)
        {
            throw new NotFoundException("Entity Instance was not found");
        }
        EntityConfiguration? entityConfiguration = await _entityConfigurationRepository.LoadAsync(
            entityInstance.EntityConfigurationId.ToString(),
            entityInstance.EntityConfigurationId.ToString(),
            cancellationToken
        );
        if (entityConfiguration == null)
        {
            throw new NotFoundException("Entity Configuration was not found");
        }
        var validationErrors = new Dictionary<string, string[]>();

        // Remove attributes
        IEnumerable<AttributeInstance> attributesToRemove = entityInstance.Attributes
            .ExceptBy(
                updateRequest.Attributes.Select(x => x.ConfigurationAttributeMachineName),
                x => x.ConfigurationAttributeMachineName
            );

        foreach (AttributeInstance? attribute in attributesToRemove)
        {
            AttributeConfiguration? attrConfiguration = entityConfiguration.Attributes.First(c => c.MachineName == attribute.ConfigurationAttributeMachineName);
            List<string>? errors = attrConfiguration.Validate(null);
            if (errors == null || errors.Count == 0)
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
            AttributeConfiguration? attrConfig = entityConfiguration.Attributes.FirstOrDefault(c => c.MachineName == newAttributeRequest.ConfigurationAttributeMachineName);
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

    #endregion
}
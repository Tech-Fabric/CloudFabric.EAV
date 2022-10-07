using AutoMapper;
using AutoMapper.Internal.Mappers;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Models.ViewModels.EAV;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore.Persistence;
using Microsoft.Extensions.Logging;

namespace CloudFabric.EAV.Service;

public class EAVService : IEAVService
{
    private readonly ILogger<EAVService> _logger;
    private readonly IMapper _mapper;
    private readonly AggregateRepository<EntityConfiguration> _entityConfigurationRepository;
    private readonly AggregateRepository<EntityInstance> _entityInstanceRepository;

    private readonly EventUserInfo _userInfo;
    //private readonly IProjectionRepository<EntityConfigurationProjection> _
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

    public async Task<EntityConfigurationViewModel> GetEntityConfiguration(Guid id, string partitionKey)
    {
        var entityConfiguration = await _entityConfigurationRepository.LoadAsync(id.ToString(), partitionKey);

        return _mapper.Map<EntityConfigurationViewModel>(entityConfiguration);
    }
    
    // public async Task<List<EntityConfigurationViewModel>> ListEntityConfigurations(int take, int skip = 0)
    // {
    //     var records = await _entityConfigurationRepository.
    //         .GetQuery()
    //         .Take(take)
    //         .Skip(skip)
    //         .ToListAsync();
    //
    //     return _mapper.Map<List<EntityConfigurationViewModel>>(records);
    // }

    public async Task<EntityConfigurationViewModel> CreateEntityConfiguration(Guid userId, EntityConfigurationCreateRequest entityConfigurationCreateRequest, CancellationToken cancellationToken)
    {
        var entityConfiguration = new EntityConfiguration(
            Guid.NewGuid(),
            _mapper.Map<List<LocalizedString>>(entityConfigurationCreateRequest.Name),
            entityConfigurationCreateRequest.MachineName,
            _mapper.Map<List<AttributeConfiguration>>(entityConfigurationCreateRequest.Attributes)
        );
        var created = await _entityConfigurationRepository.SaveAsync(_userInfo, entityConfiguration, cancellationToken);
        
        return _mapper.Map<EntityConfigurationViewModel>(entityConfiguration);
    }

    public Task UpdateEntityConfiguration(Guid userId, EntityConfigurationUpdateRequest entity)
    {
        throw new NotImplementedException();
    }

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

    public async Task<EntityInstanceViewModel> CreateEntityInstance(Guid userId, EntityInstanceCreateRequest entity)
    {
        var entityInstance = new EntityInstance(
            Guid.NewGuid(),
            entity.EntityConfigurationId,
            _mapper.Map<List<AttributeInstance>>(entity.Attributes)
        );

        var entityConfiguration = await GetEntityConfiguration(entityInstance.EntityConfigurationId, entityInstance.Id);

        if (entityConfiguration != null)
        {
            foreach (var a in entityConfiguration.Attributes)
            {
                var attributeValue = entityInstance.Attributes.FirstOrDefault(attr => a.MachineName == attr.ConfigurationAttributeMachineName);


            }
        }

        var created = await _entityInstanceRepository.SaveAsync(_userInfo, entityInstance);

        return _mapper.Map<EntityInstanceViewModel>(entityInstance);
    }

    public async Task<EntityInstanceViewModel> GetEntityInstance(Guid id, string partitionKey)
    {
        var entityInstance = await _entityInstanceRepository.LoadAsync(id.ToString(), partitionKey);

        return _mapper.Map<EntityInstanceViewModel>(entityInstance);
    }

    public Task UpdateEntityInstance(Guid userId, EntityInstanceUpdateRequest entity)
    {
        throw new NotImplementedException();
    }
}
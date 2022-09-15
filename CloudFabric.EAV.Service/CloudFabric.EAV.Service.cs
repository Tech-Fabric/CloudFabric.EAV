namespace CloudFabric.EAV.Service;

public class EAVService : IEAVService
{
    private readonly ILogger<EAVService> _logger;
    private readonly IMapper _mapper;
    private readonly EntityConfigurationRepository _entityConfigurationRepository;
    private readonly EntityInstanceRepository _entityInstanceRepository;

    public EAVService(
        ILogger<EAVService> logger,
        IMapper mapper,
        EntityConfigurationRepository entityConfigurationRepository,
        EntityInstanceRepository entityInstanceRepository
        )
    {
        _logger = logger;
        _mapper = mapper;
        _entityConfigurationRepository = entityConfigurationRepository;
        _entityInstanceRepository = entityInstanceRepository;
    }

    public async Task<EntityConfigurationViewModel> GetEntityConfiguration(Guid id)
    {
        var entityConfiguration = await _entityConfigurationRepository
            .GetOne(id);

        return _mapper.Map<EntityConfigurationViewModel>(entityConfiguration);
    }

    public async Task<List<EntityConfigurationViewModel>> ListEntityConfigurations(int take, int skip = 0)
    {
        var records = await _entityConfigurationRepository
            .GetQuery()
            .Take(take)
            .Skip(skip)
            .ToListAsync();

        return _mapper.Map<List<EntityConfigurationViewModel>>(records);
    }

    public async Task<EntityConfigurationViewModel> CreateEntityConfiguration(Guid userId, EntityConfigurationCreateRequest entity)
    {
        var entityConfiguration = _mapper.Map<EntityConfiguration>(entity);

        var created = await _entityConfigurationRepository.Create(entityConfiguration, userId);

        return _mapper.Map<EntityConfigurationViewModel>(created);
    }

    public Task UpdateEntityConfiguration(Guid userId, EntityConfigurationUpdateRequest entity)
    {
        throw new NotImplementedException();
    }

    public async Task<List<EntityInstanceViewModel>> ListEntityInstances(string entityConfigurationMachineName, int take, int skip = 0)
    {
        var records = await _entityInstanceRepository
            .GetQuery()
            .Where(e => e.EntityConfiguration.MachineName == entityConfigurationMachineName)
            .Take(take)
            .Skip(skip)
            .ToListAsync();

        return _mapper.Map<List<EntityInstanceViewModel>>(records);
    }

    public async Task<List<EntityInstanceViewModel>> ListEntityInstances(Guid entityConfigurationId, int take, int skip = 0)
    {
        var records = await _entityInstanceRepository
            .GetQuery()
            .Where(e => e.EntityConfigurationId == entityConfigurationId)
            .Take(take)
            .Skip(skip)
            .ToListAsync();

        return _mapper.Map<List<EntityInstanceViewModel>>(records);
    }

    public async Task<EntityInstanceViewModel> CreateEntityInstance(Guid userId, EntityInstanceCreateRequest entity)
    {
        var entityInstance = _mapper.Map<EntityInstance>(entity);

        var entityConfiguration = await GetEntityConfiguration(entityInstance.EntityConfigurationId);

        if (entityConfiguration != null)
        {
            foreach (var a in entityConfiguration.Attributes)
            {
                var attributeValue = entityInstance.Attributes.FirstOrDefault(attr => a.MachineName == attr.ConfigurationAttributeMachineName);


            }
        }

        var created = await _entityInstanceRepository.Create(entityInstance, userId);

        return _mapper.Map<EntityInstanceViewModel>(created);
    }

    public async Task<EntityInstanceViewModel> GetEntityInstance(Guid id)
    {
        var entityInstance = await _entityInstanceRepository
            .GetOne(id);

        return _mapper.Map<EntityInstanceViewModel>(entityInstance);
    }

    public Task UpdateEntityInstance(Guid userId, EntityInstanceUpdateRequest entity)
    {
        throw new NotImplementedException();
    }
}
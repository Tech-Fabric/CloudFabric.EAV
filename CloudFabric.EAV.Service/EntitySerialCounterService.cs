using AutoMapper;

using CloudFabric.EAV.Domain;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Models.ViewModels.Attributes;
using CloudFabric.EventSourcing.Domain;

namespace CloudFabric.EAV.Service;

public class CounterActionResponce
{
    public CounterActionResponce(Guid entityConfiguration, Guid attributeConfigurationId)
    {
        EntityConfigurationId = entityConfiguration;
        AttributeConfigurationId = attributeConfigurationId;
    }

    public CounterActionStatus Status { get; set; }

    public Guid EntityConfigurationId { get; }

    public Guid AttributeConfigurationId { get; }
}

public class EntitySerialCounterService
{
    private readonly IStoreRepository _storeRepository;
    private readonly IMapper _mapper;

    public EntitySerialCounterService(IStoreRepository storeRepository, IMapper mapper)
    {
        _storeRepository = storeRepository;
        _mapper = mapper;
    }

    #region Initializers

    /// <summary>
    /// Initalize counters for all serial attribute confgirurations within entity.
    /// Already initialized counters will not be overriten or changed.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="attributesConfigurations"></param>
    public async Task InitializeEntityConfigurationCounters(Guid entityConfigurationId, List<AttributeConfigurationViewModel> attributesConfigurations)
    {
        var serialAttributeConfigurations =
            attributesConfigurations
            .Where(x => x.ValueType == EavAttributeType.Serial)
            .ToList();

        foreach (var attribute in serialAttributeConfigurations)
        {
            var counter = await InitializeEntityConfigurationCounterIfSerialAttribute(entityConfigurationId, attribute);
        }
    }

    /// <summary>
    /// Initalize counter for attribute configuration within entity configuration if attribute is serial.
    /// Already initialized counter will not be overriten or changed.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="attributeConfiguration"></param>
    /// <returns>Initialized counter if it has not been initialized previously and attribute is serial attribute, otherwise null.</returns>
    public async Task<Counter?> InitializeEntityConfigurationCounterIfSerialAttribute(Guid entityConfigurationId, AttributeConfiguration attributeConfiguration)
    {
        var serialConfigurationViewModel = _mapper.Map<AttributeConfigurationViewModel>(attributeConfiguration);

        return await InitializeEntityConfigurationCounterIfSerialAttribute(entityConfigurationId, serialConfigurationViewModel);
    }

    /// <summary>
    /// Initalize counter for attribute configuration within entity configuration if attribute is serial.
    /// Already initialized counter will not be overriten or changed.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="attributeConfiguration"></param>
    /// <returns>Initialized counter if it has not been initialized previously and attribute is serial attribute, otherwise null.</returns>
    public async Task<Counter?> InitializeEntityConfigurationCounterIfSerialAttribute(Guid entityConfigurationId, AttributeConfigurationViewModel attributeConfiguration)
    {
        var serialAttributeConfiguration = attributeConfiguration as SerialAttributeConfigurationViewModel;

        if (serialAttributeConfiguration == null)
        {
            return null;
        }

        return await InitializeEntityConfigurationSerialAttributeCounter(entityConfigurationId, serialAttributeConfiguration);
    }

    /// <summary>
    /// Initalize counter for serial attribute configuration withing entity configuration.
    /// Already initialized counter will not be overriten or changed.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="serialAttributeConfiguration"></param>
    /// <returns>Initialized counter if it has not been initialized previously, otherwise null.</returns>
    public async Task<Counter?> InitializeEntityConfigurationSerialAttributeCounter(Guid entityConfigurationId, SerialAttributeConfiguration serialAttributeConfiguration)
    {
        var serialAttributeConfigurationViewModel = _mapper.Map<SerialAttributeConfigurationViewModel>(serialAttributeConfiguration);

        return await InitializeEntityConfigurationSerialAttributeCounter(entityConfigurationId, serialAttributeConfigurationViewModel);

    }

    /// <summary>
    /// Initalize counter for serial attribute configuration withing entity configuration.
    /// Already initialized counter will not be overriten or changed.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="serialAttributeConfiguration"></param>
    /// <returns>Initialized counter if it has not been initialized previously, otherwise null.</returns>
    public async Task<Counter?> InitializeEntityConfigurationSerialAttributeCounter(Guid entityConfigurationId, SerialAttributeConfigurationViewModel serialAttributeConfiguration)
    {
        var existingCounter = await LoadCounter(entityConfigurationId, serialAttributeConfiguration.Id);

        if (existingCounter != null)
        {
            return null;
        }

        var counter = new Counter(serialAttributeConfiguration.StartingNumber, DateTime.UtcNow, serialAttributeConfiguration.Id);

        await _storeRepository.UpsertItem(
             string.Concat(entityConfigurationId, serialAttributeConfiguration.Id),
             string.Concat(entityConfigurationId, serialAttributeConfiguration.Id),
             counter
        );

        return counter;
    }

    #endregion

    /// <summary>
    /// Load counter for specified attribute configuration within entity configuration.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="attributeId"></param>
    /// <returns>Counter or null if not exists.</returns>
    public async Task<Counter?> LoadCounter(Guid entityConfigurationId, Guid attributeId)
    {
        return await _storeRepository.LoadItem<Counter>(
             string.Concat(entityConfigurationId, attributeId),
             string.Concat(entityConfigurationId, attributeId)
        );
    }

    /// <summary>
    /// Update and validate counter, related to specified attribute configuration within entity configuration.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="attributeId"></param>
    /// <param name="newCounter"></param>
    /// <returns>Flag that idnicates existance of counter and errors, that are null if everything is okay.</returns>
    public async Task<(bool isExists, List<string>? errors)> UpdateCounterIfExists(Guid entityConfigurationId, Guid attributeId, Counter newCounter)
    {
        var counter = await LoadCounter(entityConfigurationId, attributeId);

        if (counter == null)
        {
            return (false, null);
        }

        List<string> errors = new();

        if (newCounter.NextValue < counter.NextValue)
        {
            errors.Add("Wrong counter sequence");
            return (true, errors);
        }

        await _storeRepository.UpsertItem(
             string.Concat(entityConfigurationId, attributeId),
             string.Concat(entityConfigurationId, attributeId),
             counter
        );

        return (true, null);
    }

    /// <summary>
    /// Save counter.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="attributeConfigurationId"></param>
    /// <param name="updatedCounter"></param>
    /// <returns>Counter action responce with status to indicate result.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<CounterActionResponce> SaveCounter(Guid entityConfigurationId, Guid attributeConfigurationId, Counter updatedCounter)
    {
        CounterActionResponce response = new(entityConfigurationId, attributeConfigurationId);

        Counter? counter = await LoadCounter(entityConfigurationId, attributeConfigurationId);

        if (counter == null)
        {
            throw new ArgumentNullException("Failed to save counter - make sure it was initialized");
        }

        if (updatedCounter.TimeStamp != counter.TimeStamp)
        {
            response.Status = CounterActionStatus.Conflict;

            return response;
        }

        counter.SetTimeStamp(DateTime.UtcNow);

        await _storeRepository.UpsertItem(
            string.Concat(entityConfigurationId, attributeConfigurationId),
            string.Concat(entityConfigurationId, attributeConfigurationId),
            updatedCounter
        );

        response.Status = CounterActionStatus.Saved;

        return response;
    }
}

using CloudFabric.EAV.Domain.GeneratedValues;
using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Models;
using CloudFabric.EAV.Models.ViewModels.Attributes;
using CloudFabric.EAV.Service.Abstractions;
using CloudFabric.EventSourcing.Domain;

namespace CloudFabric.EAV.Service;

public class SerialCounterService : IGeneratedValueService<Counter>
{
    private readonly IStoreRepository _storeRepository;

    public SerialCounterService(IStoreRepository storeRepository)
    {
        _storeRepository = storeRepository;
    }

    /// <summary>
    /// Create counter for serial attribute configuration withing entity configuration.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="attributeConfiguration"></param>
    /// <returns>New counter if it has not been created previously, otherwise null.</returns>
    public async Task<Counter?> Create(Guid entityConfigurationId, AttributeConfigurationViewModel attributeConfiguration)
    {
        if (attributeConfiguration is not SerialAttributeConfigurationViewModel serialAttributeConfiguration)
        {
            return null;
        }

        var existingCounter = await Load(entityConfigurationId, serialAttributeConfiguration.Id);

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

    /// <summary>
    /// Load counter for specified attribute configuration within entity configuration.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="attributeConfigurationId"></param>
    /// <returns>Counter or null if not exists.</returns>
    public async Task<Counter?> Load(Guid entityConfigurationId, Guid attributeConfigurationId)
    {
        return await _storeRepository.LoadItem<Counter>(
             string.Concat(entityConfigurationId, attributeConfigurationId),
             string.Concat(entityConfigurationId, attributeConfigurationId)
        );
    }

    /// <summary>
    /// Save counter.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="attributeConfigurationId"></param>
    /// <param name="updatedCounter"></param>
    /// <returns>Action response with status to indicate result.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<GeneratedValueActionResponse> Save(Guid entityConfigurationId, Guid attributeConfigurationId, Counter updatedCounter)
    {
        GeneratedValueActionResponse response = new(entityConfigurationId, attributeConfigurationId)
        {
            GeneratedValueType = typeof(Counter)
        };

        Counter? counter = await Load(entityConfigurationId, attributeConfigurationId);

        if (counter == null)
        {
            throw new ArgumentNullException("Failed to save counter - make sure it was initialized");
        }

        if (updatedCounter.Timestamp != counter.Timestamp)
        {
            response.Status = GeneratedValueActionStatus.Conflict;

            return response;
        }

        counter.SetTimestamp(DateTime.UtcNow);

        await _storeRepository.UpsertItem(
            string.Concat(entityConfigurationId, attributeConfigurationId),
            string.Concat(entityConfigurationId, attributeConfigurationId),
            updatedCounter
        );

        response.Status = GeneratedValueActionStatus.Saved;

        return response;
    }
}

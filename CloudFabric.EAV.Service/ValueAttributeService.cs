using AutoMapper;

using CloudFabric.EAV.Domain.GeneratedValues;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Models;
using CloudFabric.EAV.Models.ViewModels.Attributes;
using CloudFabric.EAV.Options;

using Microsoft.Extensions.Options;

namespace CloudFabric.EAV.Service;

public class ValueAttributeService
{
    private readonly SerialCounterService _entitySerialCounterService;
    private readonly IMapper _mapper;
    private readonly ValueAttributeServiceOptions _options;

    public ValueAttributeService(
        SerialCounterService entitySerialCounterService,
        IMapper mapper,
        IOptions<ValueAttributeServiceOptions>? options = null
    )
    {
        _entitySerialCounterService = entitySerialCounterService;
        _mapper = mapper;
        _options = options?.Value ?? new ValueAttributeServiceOptions();
    }

    /// <summary>
    /// Initalize generating values for all attribute confgirurations within entity based on attribute type.
    /// Already initialized values will not be overriten or changed.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="attributesConfigurations"></param>
    internal async Task InitializeEntityConfigurationGeneratedValues(Guid entityConfigurationId, List<AttributeConfigurationViewModel> attributesConfigurations)
    {
        foreach (var attribute in attributesConfigurations)
        {
            await InitializeGeneratedValue(entityConfigurationId, attribute);
        }
    }

    /// <summary>
    /// Initialize generating value for attribute configuration within entity configuration based on attribute type.
    /// Already initialized values will not be overriten or changed.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="attributeConfiguration"></param>
    internal async Task InitializeGeneratedValue(Guid entityConfigurationId, AttributeConfiguration attributeConfiguration)
        => await InitializeGeneratedValue(entityConfigurationId, _mapper.Map<AttributeConfigurationViewModel>(attributeConfiguration));

    /// <summary>
    /// Initalize generating value for attribute configuration within entity configuration based on attribute type.
    /// Already initialized values will not be overriten or changed.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="attributeConfiguration"></param>
    internal async Task InitializeGeneratedValue(Guid entityConfigurationId, AttributeConfigurationViewModel attributeConfiguration)
    {
        switch (attributeConfiguration.ValueType)
        {
            case (EavAttributeType.Serial):
                {
                    var serialAttributeConfiguration = attributeConfiguration as SerialAttributeConfigurationViewModel;

                    if (serialAttributeConfiguration == null)
                    {
                        break;
                    }

                    await _entitySerialCounterService.Create(entityConfigurationId, serialAttributeConfiguration);

                    break;
                }
        }
    }

    /// <summary>
    /// Initialize attribute instance value with entity generated value.
    /// </summary>
    /// <remarks>
    /// Make sure to use this service .Save* method to stock generated value with changed state.
    /// </remarks>
    /// <param name="entityConfiguration"></param>
    /// <param name="attributeConfiguration"></param>
    /// <param name="attributeInstance"></param>
    /// <returns>Generated value with changed values if attribute value was generated with it, otherwise null.</returns>
    /// <exception cref="ArgumentException"></exception>
    internal async Task<IGeneratedValueInfo?> GenerateAttributeInstanceValue(
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
                        return null;
                    }

                    EntityConfigurationAttributeReference? entityAttribute = entityConfiguration.Attributes
                        .FirstOrDefault(x => x.AttributeConfigurationId == attributeConfiguration.Id);

                    if (entityAttribute == null)
                    {
                        return null;
                    }

                    var serialAttributeConfiguration = attributeConfiguration as SerialAttributeConfiguration;

                    var serialInstance = attributeInstance as SerialAttributeInstance;

                    if (serialAttributeConfiguration == null || serialInstance == null)
                    {
                        return null;
                    }

                    var counter = await _entitySerialCounterService.Load(entityConfiguration.Id, attributeConfiguration.Id);

                    if (counter == null)
                    {
                        counter = await _entitySerialCounterService.Create(
                            entityConfiguration.Id,
                            _mapper.Map<AttributeConfigurationViewModel>(attributeConfiguration)
                        );
                    }

                    serialInstance.Value = counter!.NextValue;

                    counter.Step(serialAttributeConfiguration.Increment);

                    return counter;
                }
        }

        return null;
    }

    /// <summary>
    /// Update entity configuration generated value.
    /// </summary>
    /// <remarks>
    /// Specialized method to update entity configuration generated values with updating arrtibute instance value -
    /// this means new instance value is not out of the generate value logic, and can be overwritten to it.
    /// Make sure to use this service .Save* method to stock generated value with changed state.
    /// </remarks>
    /// <param name="entityConfiguration"></param>
    /// <param name="attributeConfiguration"></param>
    /// <param name="attributeInstance"></param>
    /// <returns>
    /// List of validation errors or counter with changed values if everithing is okay.
    /// </returns>
    internal async Task<(IGeneratedValueInfo?, List<string>?)> UpdateGeneratedValueDuringInstanceUpdate(
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
                        return (null, null);
                    }

                    var validationErrors = new List<string>();

                    EntityConfigurationAttributeReference? entityAttribute = entityConfiguration.Attributes
                        .FirstOrDefault(x => x.AttributeConfigurationId == attributeConfiguration.Id);

                    if (entityAttribute == null)
                    {
                        validationErrors.Add("Attribute configuration is not found");
                        return (null, validationErrors);
                    }

                    var serialAttributeConfiguration = attributeConfiguration as SerialAttributeConfiguration;

                    var serialInstance = attributeInstance as SerialAttributeInstance;

                    if (serialAttributeConfiguration == null || serialInstance == null)
                    {
                        validationErrors.Add("Invalid attribute type.");
                    }

                    if (serialInstance != null && !serialInstance.Value.HasValue)
                    {
                        validationErrors.Add("Updating serial number value can not be empty.");
                    }

                    if (validationErrors.Count > 0)
                    {
                        return (null, validationErrors);
                    }

                    var counter = await _entitySerialCounterService.Load(entityConfiguration.Id, attributeConfiguration.Id);

                    if (counter == null)
                    {
                        validationErrors.Add("Counter is not found.");
                    }

                    if (serialInstance!.Value <= counter!.NextValue)
                    {
                        // TO DO: Add validation and possibility to update serial value if value less than existing one
                        validationErrors.Add("Serial number value can not be less or equal than the already existing one.");
                        return (null, validationErrors);
                    }

                    counter.NextValue = serialInstance.Value.Value + serialAttributeConfiguration.Increment;

                    return (counter, null);
                }
        }
        return (null, null);
    }

    /// <summary>
    /// Save all generated values with changed state within entity.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="info"></param>
    /// <returns>Action responses list with statuses to indicate result.</returns>
    public async Task<List<GeneratedValueActionResponse>> SaveValues(Guid entityConfigurationId, List<IGeneratedValueInfo?> info)
    {
        List<GeneratedValueActionResponse> valueActionReponses = new();

        foreach (var item in info.Where(x => x != null))
        {
            var response = await SaveValue(entityConfigurationId, item!);
            valueActionReponses.Add(response);
        }

        return valueActionReponses;
    }

    /// <summary>
    /// Save all generated value with changed state within entity.
    /// </summary>
    /// <param name="entityConfigurationId"></param>
    /// <param name="info"></param>
    /// <returns>Action response with status to indicate result.</returns>
    internal async Task<GeneratedValueActionResponse> SaveValue(Guid entityConfigurationId, IGeneratedValueInfo info)
    {
        if (info is Counter counter)
        {
            var response = await _entitySerialCounterService.Save(entityConfigurationId, info.AttributeConfidurationId, counter);

            int loopCounter = 0;

            if (response.Status == GeneratedValueActionStatus.Conflict)
            {
                do
                {
                    if (loopCounter == _options.ActionMaxCountAttempts)
                    {
                        response.Status = GeneratedValueActionStatus.Failed;

                        break;
                    }

                    var actualStateCounter = await _entitySerialCounterService.Load(response.EntityConfigurationId, response.AttributeConfigurationId);

                    actualStateCounter!.Step(actualStateCounter.LastIncrement!.Value);

                    var repeatedCounterSaveResponse = await _entitySerialCounterService.Save(entityConfigurationId, actualStateCounter.AttributeConfidurationId, actualStateCounter);

                    response.Status = repeatedCounterSaveResponse.Status;

                    loopCounter++;

                } while (response.Status != GeneratedValueActionStatus.Saved);
            }

            return response;
        }

        return new GeneratedValueActionResponse(entityConfigurationId, info.AttributeConfidurationId)
        {
            Status = GeneratedValueActionStatus.NoAction
        };
    }
}

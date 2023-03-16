using System.Globalization;
using System.Text.Json;

using AutoMapper;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EventSourcing.Domain;

namespace CloudFabric.EAV.Service.Serialization;

public class EntityInstanceCreateUpdateRequestFromJsonDeserializer
{
    private readonly IMapper _mapper;
    private readonly AggregateRepository<AttributeConfiguration> _attributeConfigurationRepository;

    public EntityInstanceCreateUpdateRequestFromJsonDeserializer(
        IMapper mapper,
        AggregateRepository<AttributeConfiguration> attributeConfigurationRepository
    )
    {
        _mapper = mapper;
        _attributeConfigurationRepository = attributeConfigurationRepository;
    }

    public async Task<(EntityInstanceCreateRequest?, ValidationErrorResponse?)> DeserializeEntityInstanceCreateRequest(
        Guid entityConfigurationId,
        Guid tenantId,
        List<AttributeConfiguration> attributesConfigurations,
        JsonDocument record
    )
    {
        var validationErrors = new Dictionary<string, string[]>();

        var entityInstance = new EntityInstanceCreateRequest
        {
            TenantId = tenantId,
            EntityConfigurationId = entityConfigurationId,
            Attributes = new List<AttributeInstanceCreateUpdateRequest>()
            // Attributes = attributesConfigurations
            //     .Where(attributeConfig => record.ContainsKey(attributeConfig.MachineName))
            //     .Select(attributeConfig =>
            //         DeserializeAttribute(attributeConfig, record[attributeConfig.MachineName])
            //     )
            //     .ToList(),
            // CategoryPaths = record.ContainsKey("CategoryPaths")
            //     ? ParseCategoryPaths(record["CategoryPaths"])
            //     : new List<CategoryPathViewModel>()
        };

        foreach (var attribute in attributesConfigurations)
        {
            if (record.RootElement.TryGetProperty(attribute.MachineName, out var attributeValue))
            {
                var (deserializedAttribute, deserializationErrors) = await DeserializeAttribute(attribute, attributeValue);

                if (deserializationErrors != null)
                {
                    validationErrors.Add(attribute.MachineName, deserializationErrors.ToArray());
                }
                else if (deserializedAttribute != null)
                {
                    entityInstance.Attributes.Add(deserializedAttribute);
                }
            }
        }

        if (validationErrors.Count > 0)
        {
            return (null, new ValidationErrorResponse(validationErrors));
        }

        return (entityInstance, null);
    }

    private List<CategoryPathViewModel> ParseCategoryPaths(object? paths)
    {
        var categoryPaths = new List<CategoryPathViewModel>();
        if (paths is List<object> pathsList)
        {
            foreach (var path in pathsList)
            {
                if (path is Dictionary<string, object> pathDictionary)
                {
                    var categoryPath = new CategoryPathViewModel();
                    foreach (KeyValuePair<string, object> pathItem in pathDictionary)
                    {
                        if (pathItem.Key == "Path")
                        {
                            categoryPath.Path = (string)pathItem.Value;
                        }
                        else if (pathItem.Key == "TreeId")
                        {
                            categoryPath.TreeId = (Guid)pathItem.Value;
                        }
                    }

                    categoryPaths.Add(categoryPath);
                }
            }
        }
        else if (paths is List<CategoryPathViewModel> pathsListOriginal)
        {
            categoryPaths = pathsListOriginal;
        }

        return categoryPaths;
    }

    private async Task<(AttributeInstanceCreateUpdateRequest?, List<string>?)> DeserializeAttribute(
        AttributeConfiguration attributeConfiguration,
        JsonElement attributeValue
    )
    {
        AttributeInstanceCreateUpdateRequest? attributeInstance = null;
        List<string>? validationErrors = null;

        switch (attributeConfiguration.ValueType)
        {
            case EavAttributeType.Array:
                attributeInstance = new ArrayAttributeInstanceCreateUpdateRequest()
                {
                    ConfigurationAttributeMachineName = attributeConfiguration.MachineName,
                    Items = new List<AttributeInstanceCreateUpdateRequest>()
                };

                if (attributeValue.ValueKind != JsonValueKind.Array)
                {
                    return (null,
                        new List<string>()
                        {
                            $"{attributeConfiguration.MachineName} is expected to be an array, " +
                            $"{attributeValue.ValueKind} received."
                        });
                }

                var itemsAttributeConfigurationId = ((attributeConfiguration as ArrayAttributeConfiguration)!)
                    .ItemsAttributeConfigurationId;

                var arrayItemsAttributeConfiguration = await _attributeConfigurationRepository
                    .LoadAsync(itemsAttributeConfigurationId, itemsAttributeConfigurationId.ToString());

                if (arrayItemsAttributeConfiguration == null)
                {
                    return (null, new List<string>() { "Array items attribute configuration was not found" });
                }

                var arrayElements = attributeValue.EnumerateArray().ToList();

                for(var i =0; i<arrayElements.Count; i++)
                {

                    var (deserializedElement, deserializationErrors) = await DeserializeAttribute(
                        arrayItemsAttributeConfiguration, arrayElements[i]
                    );

                    if (deserializationErrors != null)
                    {
                        return (null, deserializationErrors);
                    }

                    ((attributeInstance as ArrayAttributeInstanceCreateUpdateRequest)!).Items.Add(deserializedElement!);
                }

                // if (attributeValue != null)
                // {
                //     (attributeInstance! as ArrayAttributeInstanceViewModel)!.Items =
                //         (attributeValue as List<object?>)!
                //         .Select(av =>
                //             DeserializeAttribute(
                //                 attributeConfiguration.MachineName,
                //                 (attributeConfiguration as ArrayAttributeConfiguration)!.ItemsType,
                //                 av
                //             )
                //         )
                //         .ToList();
                // }

                break;
            default:
                (attributeInstance, validationErrors) = DeserializeAttribute(
                    attributeConfiguration.MachineName,
                    attributeConfiguration.ValueType,
                    attributeValue
                );
                break;
        }

        return (attributeInstance, validationErrors);
    }

    private (AttributeInstanceCreateUpdateRequest?, List<string>?) DeserializeAttribute(
        string attributeMachineName,
        EavAttributeType attributeType,
        JsonElement attributeValue
    )
    {
        AttributeInstanceCreateUpdateRequest? attributeInstance;
        switch (attributeType)
        {
            case EavAttributeType.Array:
                throw new InvalidOperationException(
                    "Please use another overload method which accepts whole " +
                    "AttributeConfiguration object to deserialize arrays."
                );
            case EavAttributeType.DateRange:
                attributeInstance = new DateRangeAttributeInstanceCreateUpdateRequest
                {
                    //Value = _mapper.Map<DateRangeAttributeInstanceValueCreateUpdateRequest>(attributeValue)
                };
                break;
            case EavAttributeType.Image:
                attributeInstance = new ImageAttributeInstanceCreateUpdateRequest
                {
                    Value = JsonSerializer.Deserialize<ImageAttributeValueCreateUpdateRequest>(
                        attributeValue,
                        new JsonSerializerOptions()
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        }
                    )
                };
                break;
            case EavAttributeType.LocalizedText:
                var localizedText = new LocalizedTextAttributeInstanceCreateUpdateRequest
                {
                    Value = new System.Collections.Generic.List<LocalizedStringCreateRequest>()
                };

                if (attributeValue.ValueKind == JsonValueKind.Array)
                {
                    foreach (var arrayElement in attributeValue.EnumerateArray())
                    {
                        foreach (var element in arrayElement.EnumerateObject())
                        {
                            try
                            {
                                var cultureInfo = CultureInfo.GetCultureInfo(element.Name);
                                localizedText.Value.Add(
                                    new LocalizedStringCreateRequest()
                                    {
                                        CultureInfoId = cultureInfo.LCID, String = attributeValue.GetString()!
                                    }
                                );
                            }
                            catch (CultureNotFoundException ex)
                            {
                                return (
                                    null,
                                    new List<string>()
                                    {
                                        $"{element.Name} culture is not supported. " +
                                        $"Example: EN-us. " +
                                        $"Please use following page to find out available cultures: https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c?redirectedfrom=MSDN"
                                    }
                                );
                            }
                        }
                    }
                }
                else
                {
                    localizedText.Value.Add(
                        new LocalizedStringCreateRequest()
                        {
                            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID,
                            String = attributeValue.GetString()!
                        }
                    );
                }

                attributeInstance = localizedText;
                break;
            case EavAttributeType.Number:
                attributeInstance =
                    new NumberAttributeInstanceCreateUpdateRequest { Value = attributeValue.GetDecimal() };
                break;
            case EavAttributeType.ValueFromList:
                attributeInstance =
                    new ValueFromListAttributeInstanceCreateUpdateRequest { Value = attributeValue.GetString()! };
                break;
            case EavAttributeType.Boolean:
                attributeInstance =
                    new BooleanAttributeInstanceCreateUpdateRequest { Value = attributeValue.GetBoolean() };
                break;
            // case EavAttributeType.File:
            //     attributeInstance = new FileAttributeInstanceCreateUpdateRequest
            //     {
            //         Value = _mapper.Map<FileAttributeValueCreateUpdateRequest>(attributeValue)
            //     };
            //     break;
            case EavAttributeType.Text:
                attributeInstance =
                    new TextAttributeInstanceCreateUpdateRequest { Value = attributeValue.GetString()! };
                break;
            case EavAttributeType.Serial:
                attributeInstance = new SerialAttributeInstanceCreateUpdateRequest { };
                break;
            default:
                throw new NotSupportedException(
                    $"Unable to deserialize attribute value of type {attributeType}: not supported"
                );
        }

        attributeInstance.ConfigurationAttributeMachineName = attributeMachineName;

        return (attributeInstance, null);
    }
}

using System.Globalization;
using System.Text.Json;

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
    private readonly AggregateRepository<AttributeConfiguration> _attributeConfigurationRepository;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public EntityInstanceCreateUpdateRequestFromJsonDeserializer(
        AggregateRepository<AttributeConfiguration> attributeConfigurationRepository,
        JsonSerializerOptions jsonSerializerOptions
    )
    {
        _attributeConfigurationRepository = attributeConfigurationRepository;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public async Task<(EntityInstanceCreateRequest?, ValidationErrorResponse?)> DeserializeEntityInstanceCreateRequest(
        Guid entityConfigurationId,
        Guid tenantId,
        List<AttributeConfiguration> attributesConfigurations,
        JsonElement record
    )
    {
        (List<AttributeInstanceCreateUpdateRequest> attributes, ValidationErrorResponse? validationErrors) =
            await DeserializeAttributes(attributesConfigurations, record);

        if (validationErrors != null)
        {
            return (null, validationErrors);
        }

        var entityInstance = new EntityInstanceCreateRequest
        {
            TenantId = tenantId,
            EntityConfigurationId = entityConfigurationId,
            Attributes = attributes
        };

        return (entityInstance, null);
    }

    public async Task<(EntityInstanceUpdateRequest?, ValidationErrorResponse?)> DeserializeEntityInstanceUpdateRequest(
        Guid id,
        Guid entityConfigurrationId,
        List<AttributeConfiguration> attributesConfigurations,
        JsonElement record
    )
    {
        var entityInstance = new EntityInstanceUpdateRequest
        {
            Id = id,
            EntityConfigurationId = entityConfigurrationId
        };

        (List<AttributeInstanceCreateUpdateRequest> attributes, ValidationErrorResponse? deserializeAttributeErrors) =
            await DeserializeAttributes(attributesConfigurations, record);

        if (deserializeAttributeErrors != null)
        {
            foreach (var keyValuePair in deserializeAttributeErrors.Errors)
            {
                return (null, deserializeAttributeErrors);
            }
        }
        else
        {
            entityInstance.AttributesToAddOrUpdate = attributes;
        }

        string attributeMahineNamesToRemoveCamelCasePropName =
            char.ToLower(nameof(EntityInstanceUpdateRequest.AttributeMachineNamesToRemove)[0])
            + nameof(EntityInstanceUpdateRequest.AttributeMachineNamesToRemove).Substring(1);

        if (record.TryGetProperty(
            $"{attributeMahineNamesToRemoveCamelCasePropName}", out var attributeMachinaNamesToRemoveJson)
            && attributeMachinaNamesToRemoveJson.ValueKind != JsonValueKind.Null
        )
        {
            if (attributeMachinaNamesToRemoveJson.ValueKind != JsonValueKind.Array
            )
            {
                return (null, new ValidationErrorResponse(
                    $"{nameof(EntityInstanceUpdateRequest.AttributeMachineNamesToRemove)}",
                    $" {attributeMachinaNamesToRemoveJson.ValueKind} is an unsupported type.")
                );
            }

            var enumeratedArray = attributeMachinaNamesToRemoveJson.EnumerateArray();

            if (enumeratedArray.Count() > 0)
            {
                if (enumeratedArray.Any(x => x.ValueKind != JsonValueKind.String))
                {
                    return (null, new ValidationErrorResponse(
                        $"{nameof(EntityInstanceUpdateRequest.AttributeMachineNamesToRemove)}",
                        $"Array contains unsupported elements type.")
                    );
                }
            }

            entityInstance.AttributeMachineNamesToRemove
                = attributeMachinaNamesToRemoveJson.Deserialize<List<string>>(_jsonSerializerOptions);
        }

        return (entityInstance, null);
    }

    public async Task<(CategoryInstanceCreateRequest?, ValidationErrorResponse?)> DeserializeCategoryInstanceCreateRequest(
        Guid categoryConfigurationId,
        Guid? tenantId,
        Guid categoryTreeId,
        Guid? parentId,
        List<AttributeConfiguration> attributesConfigurations,
        JsonElement record
    )
    {
        (List<AttributeInstanceCreateUpdateRequest> attributes, ValidationErrorResponse? validationErrors) =
            await DeserializeAttributes(attributesConfigurations, record);

        if (validationErrors != null)
        {
            return (null, validationErrors);
        }

        CategoryInstanceCreateRequest categoryInstanceCreateRequest = new CategoryInstanceCreateRequest
        {
            CategoryConfigurationId = categoryConfigurationId,
            CategoryTreeId = categoryTreeId,
            ParentId = parentId,
            TenantId = tenantId,
            Attributes = attributes
        };

        return (categoryInstanceCreateRequest, null);
    }

    private async Task<(List<AttributeInstanceCreateUpdateRequest>, ValidationErrorResponse?)> DeserializeAttributes(
        List<AttributeConfiguration> attributesConfigurations, JsonElement record
    )
    {
        List<AttributeInstanceCreateUpdateRequest> attributes = new List<AttributeInstanceCreateUpdateRequest>();
        Dictionary<string, string[]> validationErrors = new Dictionary<string, string[]>();

        foreach (var attribute in attributesConfigurations)
        {
            if (record.TryGetProperty(attribute.MachineName, out var attributeValue))
            {
                var (deserializedAttribute, deserializationErrors) =
                    await DeserializeAttribute(attribute, attributeValue);

                if (deserializationErrors != null)
                {
                    validationErrors.Add(attribute.MachineName, deserializationErrors.ToArray());
                }
                else if (deserializedAttribute != null)
                {
                    attributes.Add(deserializedAttribute);
                }
            }
        }

        if (validationErrors.Count > 0)
        {
            return (attributes, new ValidationErrorResponse(validationErrors));
        }

        return (attributes, null);
    }

    private async Task<(AttributeInstanceCreateUpdateRequest?, List<string>?)> DeserializeAttribute(
        AttributeConfiguration attributeConfiguration,
        JsonElement attributeValue
    )
    {
        AttributeInstanceCreateUpdateRequest? attributeInstance = null;
        List<string>? validationErrors = null;

        try
        {
            switch (attributeConfiguration.ValueType)
            {
                case EavAttributeType.Array:
                    attributeInstance = new ArrayAttributeInstanceCreateUpdateRequest()
                    {
                        ConfigurationAttributeMachineName = attributeConfiguration.MachineName,
                        ValueType = attributeConfiguration.ValueType,
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

                    for (var i = 0; i < arrayElements.Count; i++)
                    {
                        var (deserializedElement, deserializationErrors) = await DeserializeAttribute(
                            arrayItemsAttributeConfiguration, arrayElements[i]
                        );

                        if (deserializationErrors != null)
                        {
                            return (null, deserializationErrors);
                        }

                        ((attributeInstance as ArrayAttributeInstanceCreateUpdateRequest)!).Items.Add(
                            deserializedElement!
                        );
                    }

                    break;
                default:
                    (attributeInstance, validationErrors) = DeserializeAttribute(
                        attributeConfiguration.MachineName,
                        attributeConfiguration.ValueType,
                        attributeValue
                    );
                    break;
            }
        }
        catch (InvalidOperationException ex)
        {
            return (null, new List<string>() { ex.Message });
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
                var enumeratedProps = attributeValue.EnumerateObject();

                var fromJsonProp = enumeratedProps.FirstOrDefault(
                    x => x.Name.ToLower() == nameof(DateRangeAttributeInstanceValueRequest.From).ToLower()
                );

                bool fromValueParsedSuccessfully = fromJsonProp.Value.TryGetDateTime(out var fromParsedValue);

                if (!fromValueParsedSuccessfully)
                {
                    attributeInstance = new DateRangeAttributeInstanceCreateUpdateRequest
                    {
                        Value = null
                    };
                    break;
                }

                // Since DateRangeAttributeInstance.Value.To is a nullable optional ptoperty
                // and used with all DateRangeAttributeType we must check nullable "To" field without exception throw
                var toJsonProp = enumeratedProps.FirstOrDefault(
                    x => x.Name.ToLower() == nameof(DateRangeAttributeInstanceValueRequest.To).ToLower()
                );
                if (toJsonProp.Value.ValueKind == JsonValueKind.Undefined
                    || toJsonProp.Value.ValueKind == JsonValueKind.Null)
                {
                    attributeInstance = new DateRangeAttributeInstanceCreateUpdateRequest
                    {
                        Value = new DateRangeAttributeInstanceValueRequest
                        {
                            From = fromParsedValue,
                            To = null
                        }
                    };
                    break;
                }

                bool toValueParsedSuccessfully = toJsonProp.Value.TryGetDateTime(out var toParsedValue);

                attributeInstance = new DateRangeAttributeInstanceCreateUpdateRequest
                {
                    Value = new DateRangeAttributeInstanceValueRequest
                    {
                        From = fromParsedValue,
                        To = toValueParsedSuccessfully ? toParsedValue : null
                    }
                };
                break;
            case EavAttributeType.Image:
                attributeInstance = new ImageAttributeInstanceCreateUpdateRequest
                {
                    Value = attributeValue.Deserialize<ImageAttributeValueCreateUpdateRequest>(
                        new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                    )
                };
                break;
            case EavAttributeType.LocalizedText:
                var localizedText = new LocalizedTextAttributeInstanceCreateUpdateRequest
                {
                    Value = new List<LocalizedStringCreateRequest>()
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
                                        CultureInfoId = cultureInfo.LCID,
                                        String = element.Value.GetString()!
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
                                        $"Example: en-US. " +
                                        $"Please use following page to find out available cultures: " +
                                        $"https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/" +
                                        $"a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c?redirectedfrom=MSDN"
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
                            CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID,
                            String = attributeValue.GetString()!
                        }
                    );
                }

                attributeInstance = localizedText;
                break;
            case EavAttributeType.Number:
                attributeInstance = new NumberAttributeInstanceCreateUpdateRequest
                {
                    Value = attributeValue.GetDecimal()
                };
                break;
            case EavAttributeType.ValueFromList:
                attributeInstance = new ValueFromListAttributeInstanceCreateUpdateRequest
                {
                    Value = attributeValue.GetString()!
                };
                break;
            case EavAttributeType.Boolean:
                attributeInstance = new BooleanAttributeInstanceCreateUpdateRequest
                {
                    Value = attributeValue.GetBoolean()
                };
                break;
            case EavAttributeType.File:
                attributeInstance = new FileAttributeInstanceCreateUpdateRequest
                {
                    Value = attributeValue.Deserialize<FileAttributeValueCreateUpdateRequest>(_jsonSerializerOptions)
                };
                break;
            case EavAttributeType.Text:
                attributeInstance =
                    new TextAttributeInstanceCreateUpdateRequest { Value = attributeValue.GetString()! };
                break;
            case EavAttributeType.Serial:
                attributeInstance = new SerialAttributeInstanceCreateUpdateRequest { };
                break;
            case EavAttributeType.Money:
                attributeInstance = new MoneyAttributeInstanceCreateUpdateRequest()
                {
                    Value = attributeValue.GetDecimal()
                };
                break;
            default:
                throw new NotSupportedException(
                    $"Unable to deserialize attribute value of type {attributeType}: not supported"
                );
        }

        attributeInstance.ConfigurationAttributeMachineName = attributeMachineName;
        attributeInstance.ValueType = attributeType;

        return (attributeInstance, null);
    }
}

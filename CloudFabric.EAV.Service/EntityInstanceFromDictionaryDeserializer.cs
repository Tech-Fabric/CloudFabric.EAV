using AutoMapper;

using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Models.ViewModels.Attributes;
using CloudFabric.EAV.Models.ViewModels.EAV;

namespace CloudFabric.EAV.Service;

public class EntityInstanceFromDictionaryDeserializer
{
    private readonly IMapper _mapper;

    public EntityInstanceFromDictionaryDeserializer(IMapper mapper)
    {
        _mapper = mapper;
    }

    public EntityInstanceViewModel Deserialize(
        EntityConfiguration entityConfiguration,
        List<AttributeConfiguration> attributesConfigurations,
        Dictionary<string, object?> record
    )
    {
        var entityInstance = new EntityInstanceViewModel()
        {
            Id = (Guid)record["Id"]!,
            TenantId = record.ContainsKey("TenantId") && record["TenantId"] != null
                ? (Guid)record["TenantId"]!
                : null,
            EntityConfigurationId = (Guid)record["EntityConfigurationId"]!,
            PartitionKey = (string)record["PartitionKey"]!,
            Attributes = attributesConfigurations
                .Select(attributeConfig =>
                    DeserializeAttribute(attributeConfig, record[attributeConfig.MachineName])
                )
                .ToList(),
            CategoryPath = (Dictionary<string, string>)record["CategoryPath"]!
        };
        return entityInstance;
    }

    private AttributeInstanceViewModel DeserializeAttribute(
        AttributeConfiguration attributeConfiguration,
        object? attributeValue
    )
    {
        AttributeInstanceViewModel? attributeInstance = null;

        switch (attributeConfiguration.ValueType)
        {
            case EavAttributeType.Array:
                attributeInstance = new ArrayAttributeInstanceViewModel()
                {
                    ConfigurationAttributeMachineName = attributeConfiguration.MachineName,
                    Items = new List<AttributeInstanceViewModel>()
                };

                if (attributeValue != null)
                {
                    ((attributeInstance! as ArrayAttributeInstanceViewModel)!).Items =
                        ((attributeValue as List<object?>)!)
                        .Select(av =>
                            DeserializeAttribute(
                                attributeConfiguration.MachineName,
                                ((attributeConfiguration as ArrayAttributeConfiguration)!).ItemsType,
                                av
                            )
                        )
                        .ToList();
                }

                break;
            default:
                attributeInstance = DeserializeAttribute(attributeConfiguration.MachineName, attributeConfiguration.ValueType, attributeValue);
                break;
        }

        return attributeInstance;
    }

    private AttributeInstanceViewModel DeserializeAttribute(
        string attributeMachineName,
        EavAttributeType attributeType,
        object? attributeValue
    )
    {
        AttributeInstanceViewModel? attributeInstance = null;

        switch (attributeType)
        {
            case EavAttributeType.Array:
                throw new InvalidOperationException("Please use another overload method which accepts whole "
                                                    + "AttributeConfiguration object to deserialize arrays.");
            case EavAttributeType.DateRange:
                attributeInstance = new DateRangeAttributeInstanceViewModel()
                {
                };
                break;
            case EavAttributeType.Image:
                attributeInstance = new ImageAttributeInstanceViewModel()
                {
                    Value = _mapper.Map<ImageAttributeValueViewModel>(attributeValue)
                };
                break;
            case EavAttributeType.LocalizedText:
                attributeInstance = new LocalizedTextAttributeInstanceViewModel()
                {
                    Value = _mapper.Map<List<LocalizedStringViewModel>>(attributeValue)
                };
                break;
            case EavAttributeType.Number:
                attributeInstance = new NumberAttributeInstanceViewModel()
                {
                    Value = (decimal?)attributeValue
                };
                break;
            case EavAttributeType.ValueFromList:
                attributeInstance = new ValueFromListAttributeInstanceViewModel()
                {
                    PreselectedOptionsMachineNames = new List<string>(),
                    UnavailableOptionsMachineNames = new List<string>()
                };
                break;
            case EavAttributeType.Boolean:
                attributeInstance = new BooleanAttributeInstanceViewModel()
                {
                    Value = (bool)attributeValue
                };
                break;
            case EavAttributeType.File:
                attributeInstance = new FileAttributeInstanceViewModel()
                {
                    Value = _mapper.Map<FileAttributeValueViewModel>(attributeValue)
                };
                break;
            default:
                throw new NotSupportedException(
                    $"Unable to deserialize attribute value of type {attributeType}: not supported"
                );
        }

        attributeInstance.ConfigurationAttributeMachineName = attributeMachineName;

        return attributeInstance;
    }
}
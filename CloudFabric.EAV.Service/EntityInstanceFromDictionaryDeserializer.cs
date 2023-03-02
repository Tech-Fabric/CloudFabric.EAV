using AutoMapper;

using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Service;

public class EntityInstanceFromDictionaryDeserializer
{
    private readonly IMapper _mapper;

    public EntityInstanceFromDictionaryDeserializer(IMapper mapper)
    {
        _mapper = mapper;
    }

    public EntityInstanceViewModel Deserialize(
        List<AttributeConfiguration> attributesConfigurations,
        Dictionary<string, object?> record
    )
    {
        var entityInstance = new EntityInstanceViewModel
        {
            Id = (Guid)record["Id"]!,
            TenantId = record.ContainsKey("TenantId") && record["TenantId"] != null
                ? (Guid)record["TenantId"]!
                : null,
            EntityConfigurationId = (Guid)record["EntityConfigurationId"]!,
            PartitionKey = (string)record["PartitionKey"]!,
            Attributes = attributesConfigurations
                .Where(attributeConfig => record.ContainsKey(attributeConfig.MachineName))
                .Select(attributeConfig =>
                    DeserializeAttribute(attributeConfig, record[attributeConfig.MachineName])
                )
                .ToList(),
            CategoryPaths = record.ContainsKey("CategoryPaths")
                ? ParseCategoryPaths(record["CategoryPaths"])
                : new List<CategoryPath>()
        };
        return entityInstance;
    }

    private List<CategoryPath> ParseCategoryPaths(object? paths)
    {
        var categoryPaths = new List<CategoryPath>();
        if (paths is List<object> pathsList)
        {
            foreach (var path in pathsList)
            {
                if (path is Dictionary<string, object> pathDictionary)
                {
                    var categoryPath = new CategoryPath();
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
        else if (paths is List<CategoryPath> pathsListOriginal)
        {
            categoryPaths = pathsListOriginal;
        }

        return categoryPaths;
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
                attributeInstance = new ArrayAttributeInstanceViewModel
                {
                    ConfigurationAttributeMachineName = attributeConfiguration.MachineName,
                    Items = new List<AttributeInstanceViewModel>()
                };

                if (attributeValue != null)
                {
                    (attributeInstance! as ArrayAttributeInstanceViewModel)!.Items =
                        (attributeValue as List<object?>)!
                        .Select(av =>
                            DeserializeAttribute(
                                attributeConfiguration.MachineName,
                                (attributeConfiguration as ArrayAttributeConfiguration)!.ItemsType,
                                av
                            )
                        )
                        .ToList();
                }

                break;
            default:
                attributeInstance = DeserializeAttribute(attributeConfiguration.MachineName,
                    attributeConfiguration.ValueType,
                    attributeValue
                );
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
        AttributeInstanceViewModel? attributeInstance;
        switch (attributeType)
        {
            case EavAttributeType.Array:
                throw new InvalidOperationException("Please use another overload method which accepts whole "
                                                    + "AttributeConfiguration object to deserialize arrays."
                );
            case EavAttributeType.DateRange:
                attributeInstance = new DateRangeAttributeInstanceViewModel
                {
                    Value = _mapper.Map<DateRangeAttributeInstanceValueViewModel>(attributeValue)
                };
                break;
            case EavAttributeType.Image:
                attributeInstance = new ImageAttributeInstanceViewModel
                {
                    Value = _mapper.Map<ImageAttributeValueViewModel>(attributeValue)
                };
                break;
            case EavAttributeType.LocalizedText:
                attributeInstance = new LocalizedTextAttributeInstanceViewModel
                {
                    Value = _mapper.Map<List<LocalizedStringViewModel>>(attributeValue)
                };
                break;
            case EavAttributeType.Number:
                attributeInstance = new NumberAttributeInstanceViewModel { Value = (decimal?)attributeValue };
                break;
            case EavAttributeType.ValueFromList:
                attributeInstance = new ValueFromListAttributeInstanceViewModel { Value = (string)attributeValue };
                break;
            case EavAttributeType.Boolean:
                attributeInstance = new BooleanAttributeInstanceViewModel { Value = (bool)attributeValue };
                break;
            case EavAttributeType.File:
                attributeInstance = new FileAttributeInstanceViewModel
                {
                    Value = _mapper.Map<FileAttributeValueViewModel>(attributeValue)
                };
                break;
            case EavAttributeType.Text:
                attributeInstance = new TextAttributeInstanceViewModel { Value = (string)attributeValue };
                break;
            case EavAttributeType.Serial:
                attributeInstance = new SerialAttributeInstanceViewModel { Value = (long)attributeValue };
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

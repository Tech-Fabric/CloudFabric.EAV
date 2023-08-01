using AutoMapper;

using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Service.Serialization;

public abstract class InstanceFromDictionaryDeserializer<T> where T: EntityInstanceViewModel
{
    internal IMapper _mapper { get; set; }

    public abstract T Deserialize(
        List<AttributeConfiguration> attributesConfigurations,
        Dictionary<string, object?> record
    );

    internal List<CategoryPathViewModel> ParseCategoryPaths(object? paths)
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
                        else if (pathItem.Key == "parentId")
                        {
                            categoryPath.ParentId = (Guid)pathItem.Value;
                        }
                        else if (pathItem.Key == "parentMachineName")
                        {
                            categoryPath.ParentMachineName = (string)pathItem.Value;
                        }
                    }

                    categoryPaths.Add(categoryPath);
                }
            }
        }
        else if (paths is List<CategoryPathViewModel> pathsListViewModel)
        {
            categoryPaths = pathsListViewModel;
        }
        else if (paths is List<CategoryPath> pathsListModel)
        {
            categoryPaths = _mapper.Map<List<CategoryPathViewModel>>(pathsListModel);
        }

        return categoryPaths;
    }

    internal AttributeInstanceViewModel DeserializeAttribute(
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

    internal AttributeInstanceViewModel DeserializeAttribute(
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
            case EavAttributeType.Money:
                attributeInstance = new MoneyAttributeInstanceViewModel
                {
                    Value = (decimal?)attributeValue
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

/// <summary>
/// Entities are stored as c# dictionaries in projections - something similar to json.
/// That is required to not overload search engines with additional complexity of entity instances and attributes
/// allowing us to simply store
/// photo.likes = 4 instead of photo.attributes.where(a => a.machineName == "likes").value = 4
///
/// That comes with a price though - we now have to decode json-like dictionary back to entity instance view model.
/// Also it becomes not clear where is a serialization part and where is a deserializer.
///
/// The following structure seems logical, not very understandable from the first sight however:
///
///
/// Serialization happens in <see cref="CloudFabric.EAV.Domain/Projections/EntityInstanceProjection/EntityInstanceProjectionBuilder.cs"/>
/// Projection builder creates dictionaries from EntityInstances and is responsible for storing projections data in
/// the best way suitable for search engines like elasticsearch.
///
/// The segregation of reads and writes moves our decoding code out of ProjectionBuilder
/// and even out of CloudFabric.EAV.Domain because our ViewModels are on another layer - same layer as a service.
/// That means it's a service concern to decode dictionary into a ViewModel.
///
/// <see cref="CloudFabric.EAV.Service/Serialization/EntityInstanceFromDictionaryDeserializer.cs"/>
/// </summary>
public class EntityInstanceFromDictionaryDeserializer: InstanceFromDictionaryDeserializer<EntityInstanceViewModel>
{

    public EntityInstanceFromDictionaryDeserializer(IMapper mapper)
    {
        _mapper = mapper;
    }

    public override EntityInstanceViewModel Deserialize(
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
                : new List<CategoryPathViewModel>()
        };
        return entityInstance;
    }

}

public class CategoryFromDictionaryDeserializer : InstanceFromDictionaryDeserializer<CategoryViewModel>
{

    public CategoryFromDictionaryDeserializer(IMapper mapper)
    {
        _mapper = mapper;
    }

    public override CategoryViewModel Deserialize(
        List<AttributeConfiguration> attributesConfigurations,
        Dictionary<string, object?> record
    )
    {
        var category = new CategoryViewModel()
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
                : new List<CategoryPathViewModel>(),
            MachineName = (string)record["MachineName"]!
        };
        return category;
    }
}

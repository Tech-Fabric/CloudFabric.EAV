using System.Text.Json.Serialization;

using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Json.Utilities;

namespace CloudFabric.EAV.Models.RequestModels;

[JsonConverter(typeof(PolymorphicJsonConverter<EntityAttributeConfigurationCreateUpdateRequest>))]
public abstract class EntityAttributeConfigurationCreateUpdateRequest
{
}

[JsonConverter(typeof(PolymorphicJsonConverter<AttributeConfigurationCreateUpdateRequest>))]
public class EntityAttributeConfigurationCreateUpdateReferenceRequest : EntityAttributeConfigurationCreateUpdateRequest
{
    public Guid AttributeConfigurationId { get; set; }
}

[JsonConverter(typeof(PolymorphicJsonConverter<AttributeConfigurationCreateUpdateRequest>))]
public abstract class AttributeConfigurationCreateUpdateRequest : EntityAttributeConfigurationCreateUpdateRequest
{
    public List<LocalizedStringCreateRequest> Name { get; set; }

    public List<LocalizedStringCreateRequest> Description { get; set; }

    public string MachineName { get; set; }

    public abstract EavAttributeType ValueType { get; }
    
    public bool IsRequired { get; set; }
}
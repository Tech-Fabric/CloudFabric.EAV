using System.Text.Json.Serialization;

using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Models.JsonConverters;

namespace CloudFabric.EAV.Models.RequestModels;

[JsonConverter(typeof(AttributeRequestJsonConverter<EntityAttributeConfigurationCreateUpdateRequest>))]
public abstract class EntityAttributeConfigurationCreateUpdateRequest
{
}

[JsonConverter(typeof(AttributeRequestJsonConverter<AttributeConfigurationCreateUpdateRequest>))]
public class
    EntityAttributeConfigurationCreateUpdateReferenceRequest : EntityAttributeConfigurationCreateUpdateRequest
{
    public Guid AttributeConfigurationId { get; set; }
}

[JsonConverter(typeof(AttributeRequestJsonConverter<AttributeConfigurationCreateUpdateRequest>))]
public abstract class AttributeConfigurationCreateUpdateRequest : EntityAttributeConfigurationCreateUpdateRequest
{
    public List<LocalizedStringCreateRequest> Name { get; set; }

    public List<LocalizedStringCreateRequest> Description { get; set; }

    public string? MachineName { get; set; }

    public abstract EavAttributeType ValueType { get; }

    public bool IsRequired { get; set; }

    public Guid? TenantId { get; set; }

    public string? Metadata { get; set; }
}

using CloudFabric.EAV.Enums;

namespace CloudFabric.EAV.Models;

public class GeneratedValueActionResponse
{
    public GeneratedValueActionResponse(Guid entityConfiguration, Guid attributeConfigurationId)
    {
        EntityConfigurationId = entityConfiguration;
        AttributeConfigurationId = attributeConfigurationId;
    }

    public Type? GeneratedValueType { get; set; }

    public GeneratedValueActionStatus Status { get; set; }

    public Guid EntityConfigurationId { get; }

    public Guid AttributeConfigurationId { get; }
}

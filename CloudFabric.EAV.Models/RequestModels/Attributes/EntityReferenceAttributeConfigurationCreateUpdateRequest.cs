using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class EntityReferenceAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public override EavAttributeType ValueType { get; } = EavAttributeType.EntityReference;

        public Guid ReferenceEntityConfiguration { get; set; }

        public Guid DefaultValue { get; set; }
    }
}

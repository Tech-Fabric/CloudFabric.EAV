using System;
using CloudFabric.EAV.Data.Enums;

namespace CloudFabric.EAV.Service.Models.RequestModels.Attributes
{
    public class EntityReferenceAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public override EavAttributeType ValueType { get; } = EavAttributeType.EntityReference;
        
        public Guid ReferenceEntityConfiguration { get; set; }
        
        public Guid DefaultValue { get; set; }
    }
}
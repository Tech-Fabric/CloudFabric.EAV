using CloudFabric.EAV.Data.Enums;

using System;

namespace CloudFabric.EAV.Attributes
{
    public class EntityReferenceAttributeConfiguration: AttributeConfiguration
    {
        public override EavAttributeType ValueType { get; } = EavAttributeType.EntityReference;
        
        public Guid ReferenceEntityConfiguration { get; set; }
        
        public Guid DefaultValue { get; set; }
    }
}
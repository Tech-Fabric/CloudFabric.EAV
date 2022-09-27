using CloudFabric.EAV.Data.Enums;

using System;

namespace CloudFabric.EAV.Data.Models.Attributes
{
    public class EntityReferenceAttributeConfiguration : AttributeConfiguration
    {
        public override EavAttributeType ValueType { get; } = EavAttributeType.EntityReference;

        public Guid ReferenceEntityConfiguration { get; set; }

        public Guid DefaultValue { get; set; }
    }
}
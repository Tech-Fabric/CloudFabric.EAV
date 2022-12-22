using System;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class EntityReferenceAttributeInstance : AttributeInstance
    {
        public Guid Value { get; set; }
        public override object? GetValue()
        {
            return Value;
        }
    }
}
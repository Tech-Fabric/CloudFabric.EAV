using System.Collections.Generic;
using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class NumberAttributeConfiguration : AttributeConfiguration
    {
        public float DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Number;
        
    }
}
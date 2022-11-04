using System;

using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class ArrayAttributeConfiguration : AttributeConfiguration
    {
        public override EavAttributeType ValueType { get; } = EavAttributeType.Array;

        public EavAttributeType ItemsType { get; set; }

        public AttributeConfiguration ItemsAttributeConfiguration { get; set; }
    }
}
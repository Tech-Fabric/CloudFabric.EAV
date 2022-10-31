using System;
using System.Collections.Generic;
using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class ArrayAttributeConfiguration : AttributeConfiguration
    {
        public override EavAttributeType ValueType { get; } = EavAttributeType.Array;

        public EavAttributeType ItemsType { get; set; }

        public AttributeConfiguration ItemsAttributeConfiguration { get; set; }
        public override (bool, List<string>) Validate(AttributeInstance instance)
        {
            return (true, new List<string>());
        }
    }
}
﻿using CloudFabric.EAV.Data.Enums;

namespace CloudFabric.EAV.Attributes
{
    public class ArrayAttributeConfiguration: AttributeConfiguration
    {
        public override EavAttributeType ValueType { get; } = EavAttributeType.Array;
        
        public EavAttributeType ItemsType { get; set; }

        public AttributeConfiguration ItemsAttributeConfiguration { get; set; }
    }
}
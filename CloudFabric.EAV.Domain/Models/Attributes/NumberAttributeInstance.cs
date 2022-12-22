﻿using System.Collections.Generic;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class NumberAttributeInstance : AttributeInstance
    {
        public decimal Value { get; set; }

        public override object? GetValue()
        {
            return Value;
        }
    }
}

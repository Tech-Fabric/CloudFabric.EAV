using System;
using System.Collections.Generic;
using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class NumberAttributeConfiguration : AttributeConfiguration
    {
        public float DefaultValue { get; set; }
        public float? MinimumValue { get; set; }
        public float? MaximumValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Number;
        public override (bool, List<string>) Validate(AttributeInstance instance)
        {
            var (result, errors) = (true, new List<string>());
            var numberInstance = instance as NumberAttributeInstance; 
            var floatValue = numberInstance?.Value ?? 0;
            if (floatValue < MinimumValue)
            {
                result = false;
                errors.Add($"Value should be greater or equal than {MinimumValue}");
            }

            if (floatValue > MaximumValue)
            {
                result = false;
                errors.Add($"Value should be less or equal than {MaximumValue}");
            }
            return (result, errors);
        }
    }
}
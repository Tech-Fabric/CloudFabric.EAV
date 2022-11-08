using System;
using System.Collections.Generic;
using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class NumberAttributeConfiguration : AttributeConfiguration
    {
        private const float _fltMin = 1.175494E-38f;
        public float DefaultValue { get; set; }
        public float? MinimumValue { get; set; }
        public float? MaximumValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Number;
        public override List<string> Validate(AttributeInstance instance)
        {
            var errors = base.Validate(instance);
            if (instance is not NumberAttributeInstance numberInstance)
            {
                errors.Add("Cannot validate attribute. Expected attribute type: Number)");
                return errors;
            }
            
            var floatValue = numberInstance.Value;
            if (floatValue < MinimumValue)
            {
                errors.Add($"Value should be greater or equal than {MinimumValue}");
            }

            if (floatValue > MaximumValue)
            {
                errors.Add($"Value should be less or equal than {MaximumValue}");
            }
            return errors;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as NumberAttributeConfiguration);
        }
        private bool Equals(NumberAttributeConfiguration other)
        {
            return base.Equals(other) 
                   && DefaultValue.Equals(other.DefaultValue) 
                   && Nullable.Equals(MinimumValue, other.MinimumValue) 
                   && Nullable.Equals(MaximumValue, other.MaximumValue) 
                   && ValueType == other.ValueType;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(DefaultValue, MinimumValue, MaximumValue, (int)ValueType);
        }
    }
}
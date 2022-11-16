using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Base;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class NumberAttributeConfiguration : AttributeConfiguration
    {
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
        
        public NumberAttributeConfiguration(
            Guid id, 
            string machineName, 
            List<LocalizedString> name,
            float defaultValue,
            List<LocalizedString> description = null, 
            bool isRequired = false,
            float? minimumValue = null,
            float? maximumValue = null
        ) : base(id, machineName, name, EavAttributeType.Number, description, isRequired)
        {
            DefaultValue = defaultValue;
            MinimumValue = minimumValue;
            MaximumValue = maximumValue;
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
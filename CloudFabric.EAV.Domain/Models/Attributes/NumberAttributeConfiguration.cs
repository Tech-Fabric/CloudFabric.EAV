using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class NumberAttributeConfiguration : AttributeConfiguration
    {
        public decimal DefaultValue { get; set; }
        public decimal? MinimumValue { get; set; }
        public decimal? MaximumValue { get; set; }
        
        public NumberAttributeType NumberType { get; set; } = NumberAttributeType.Integer;

        public override EavAttributeType ValueType { get; } = EavAttributeType.Number;

        public override List<string> Validate()
        {
            var errors = base.Validate();
            if (MinimumValue != null && MaximumValue != null && MinimumValue > MaximumValue)
            {
                errors.Add("Minimum value cannot be greater than Maximum value");
            }

            if (MaximumValue != null && DefaultValue > MaximumValue)
            {
                errors.Add("Default value cannot be greater than Maximum value");
            }

            if (MinimumValue != null && DefaultValue < MinimumValue)
            {
                errors.Add("Default value cannot be less than Minimum value");
            }
            return errors;
        }

        public override List<string> ValidateInstance(AttributeInstance? instance)
        {
            var errors = base.ValidateInstance(instance);

            if (instance == null)
            {
                return errors;
            }

            if (instance is not NumberAttributeInstance numberInstance)
            {
                errors.Add("Cannot validate attribute. Expected attribute type: Number)");
                return errors;
            }

            var floatValue = numberInstance.Value;

            if (NumberType == NumberAttributeType.Integer && floatValue != Math.Truncate(floatValue))
            {
                errors.Add("Value is not an integer value");
            }
            
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

        public NumberAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {
            
        }
        
        public NumberAttributeConfiguration(
            Guid id, 
            string machineName, 
            List<LocalizedString> name,
            decimal defaultValue,
            NumberAttributeType numberType,
            List<LocalizedString> description = null, 
            bool isRequired = false,
            decimal? minimumValue = null,
            decimal? maximumValue = null,
            Guid? tenantId = null,
            string? metadata = null
        ) : base(id, machineName, name, EavAttributeType.Number, description, isRequired, tenantId, metadata)
        {
            Apply(new NumberAttributeConfigurationUpdated(id, defaultValue, minimumValue, maximumValue, numberType));
        }

        public override void UpdateAttribute(AttributeConfiguration updatedAttribute)
        {
            var updated = updatedAttribute as NumberAttributeConfiguration;

            if (updated == null)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            base.UpdateAttribute(updatedAttribute);

            if (DefaultValue != updated.DefaultValue
                || MinimumValue != updated.MinimumValue
                || MaximumValue != updated.MaximumValue
                || NumberType != updated.NumberType
            )
            {
                Apply(
                    new NumberAttributeConfigurationUpdated(Id, updated.DefaultValue, updated.MinimumValue, updated.MaximumValue, updated.NumberType)
                );
            }
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
                   && NumberType == other.NumberType
                   && ValueType == other.ValueType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DefaultValue, MinimumValue, MaximumValue, NumberType, (int)ValueType);
        }
        
        #region EventHandlers

        public void On(NumberAttributeConfigurationUpdated @event)
        {
            DefaultValue = @event.DefaultValue;
            MinimumValue = @event.MinimumValue;
            MaximumValue = @event.MaximumValue;
            NumberType = @event.NumberType;
        }

        #endregion
    }
}
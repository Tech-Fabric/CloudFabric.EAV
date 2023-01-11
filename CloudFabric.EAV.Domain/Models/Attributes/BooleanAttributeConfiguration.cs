using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class BooleanAttributeConfiguration : AttributeConfiguration
    {
        public string TrueDisplayValue { get; set; }

        public string FalseDisplayValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Boolean;
        
        public override List<string> Validate(AttributeInstance? instance)
        {
            var errors = base.Validate(instance);

            if (instance == null)
            {
                return errors;
            }

            if (instance is not BooleanAttributeInstance)
            {
                errors.Add("Cannot validate attribute. Expected attribute type: Boolean");
                return errors;
            }
            
            return errors;
        }

        public BooleanAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {
        }
        
        public BooleanAttributeConfiguration(
            Guid id, 
            string machineName, 
            List<LocalizedString> name,
            string trueDisplayValue,
            string falseDisplayValue,
            List<LocalizedString> description = null, 
            bool isRequired = false,
            Guid? tenantId = null
        ) : base(id, machineName, name, EavAttributeType.Boolean, description, isRequired, tenantId)
        {
            Apply(new BooleanAttributeConfigurationUpdated(id, trueDisplayValue, falseDisplayValue));
        }

        public override void UpdateAttribute(AttributeConfiguration updatedAttribute)
        {
            var updated = updatedAttribute as BooleanAttributeConfiguration;

            if (updated == null)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            base.UpdateAttribute(updatedAttribute);

            if (TrueDisplayValue != updated.TrueDisplayValue || FalseDisplayValue != updated.FalseDisplayValue)
            {
                Apply(new BooleanAttributeConfigurationUpdated(Id, updated.TrueDisplayValue, updated.FalseDisplayValue));
            }
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as BooleanAttributeConfiguration);
        }

        private bool Equals(BooleanAttributeConfiguration other)
        {
            return base.Equals(other)
                   && TrueDisplayValue == other.TrueDisplayValue
                   && FalseDisplayValue == other.FalseDisplayValue
                   && ValueType == other.ValueType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TrueDisplayValue, FalseDisplayValue, (int)ValueType);
        }

        #region EventHandlers

        public void On(BooleanAttributeConfigurationUpdated @event)
        {
            Id = @event.AggregateId;
            TrueDisplayValue = @event.TrueDisplayValue;
            FalseDisplayValue = @event.FalseDisplayValue;
        }

        #endregion
    }
}
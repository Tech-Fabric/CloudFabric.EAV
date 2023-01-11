using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class ValueFromListAttributeConfiguration : AttributeConfiguration
    {

        public ValueFromListAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {
        }
        public ValueFromListAttributeConfiguration(Guid id,
            string machineName,
            List<LocalizedString> name,
            ValueFromListAttributeType valueFromListAttributeType,
            List<ValueFromListOptionConfiguration> valuesList,
            string? attributeMachineNameToAffect,
            List<LocalizedString> description = null,
            bool isRequired = false,
            Guid? TenantId = null) : base(id, machineName, name, EavAttributeType.ValueFromList, description, isRequired, TenantId)
        {
            ValueFromListAttributeType = valueFromListAttributeType;
            ValuesList = valuesList;
            AttributeMachineNameToAffect = attributeMachineNameToAffect;
            Apply(new ValueFromListConfigurationUpdated(id, valueFromListAttributeType, valuesList, attributeMachineNameToAffect));

        }

        public ValueFromListAttributeType ValueFromListAttributeType { get; set; }
        public List<ValueFromListOptionConfiguration> ValuesList { get; set; }
        public string? AttributeMachineNameToAffect { get; set; }
        public override EavAttributeType ValueType => EavAttributeType.ValueFromList;

        public override List<string> Validate(AttributeInstance? instance)
        {
            var errors = base.Validate(instance);

            if (instance == null)
            {
                return errors;
            }

            if (instance is not ValueFromListAttributeInstance)
            {
                errors.Add("Cannot validate attribute. Expected attribute type: Dropdown");
                return errors;
            }

            return errors;
        }

        public override void UpdateAttribute(AttributeConfiguration updatedAttribute)
        {
            var updated = updatedAttribute as ValueFromListAttributeConfiguration;

            if (updated == null)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            base.UpdateAttribute(updatedAttribute);

            if (ValueFromListAttributeType != updated.ValueFromListAttributeType
                || !ValuesList.Equals(updated.ValuesList)
                || AttributeMachineNameToAffect != updated.AttributeMachineNameToAffect
            )
            {
                Apply(new ValueFromListConfigurationUpdated(Id, updated.ValueFromListAttributeType, updated.ValuesList, updated.AttributeMachineNameToAffect));
            }
        }

        protected bool Equals(ValueFromListAttributeConfiguration other)
        {
            return base.Equals(other)
                   && ValueFromListAttributeType == other.ValueFromListAttributeType
                   && ValuesList.Equals(other.ValuesList)
                   && AttributeMachineNameToAffect == other.AttributeMachineNameToAffect;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ValueFromListAttributeConfiguration)obj);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), (int)ValueFromListAttributeType, ValuesList, AttributeMachineNameToAffect);
        }

        #region EventHandlers

        public void On(ValueFromListConfigurationUpdated @event)
        {
            if (@event.ValueFromListOptions.GroupBy(x => x.MachineName).Any(x => x.Count() > 1)
                || @event.ValueFromListOptions.GroupBy(x => x.Name).Any(x => x.Count() > 1))
            {
                throw new Exception("Identical options' names not allowed");
            }

            ValueFromListAttributeType = @event.ValueFromListAttributeType;
            ValuesList = @event.ValueFromListOptions;
            AttributeMachineNameToAffect = @event.AttributeMachineNameToAffect;
        }

        #endregion
    }
}
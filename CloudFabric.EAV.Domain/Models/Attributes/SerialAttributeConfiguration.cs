using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class SerialAttributeConfiguration : AttributeConfiguration
    {
        public long StartingNumber { get; set; }

        public int Increment { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Serial;

        public override List<string> ValidateInstance(AttributeInstance? instance)
        {
            var errors = base.ValidateInstance(instance);

            if (instance == null)
            {
                return errors;
            }

            if (instance is not SerialAttributeInstance)
            {
                errors.Add("Cannot validate attribute. Expected attribute type: Serial");
                return errors;
            }

            return errors;
        }

        public override List<string> Validate()
        {
            var errors = base.Validate();
            if (Increment == 0)
            {
                errors.Add("Increment for serial number must not be 0");
            }
            return errors;
        }

        public SerialAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {

        }

        public SerialAttributeConfiguration(Guid id,
            string machineName,
            List<LocalizedString> name,
            long startingNumber,
            int increment,
            List<LocalizedString> description = null,
            bool isRequired = false,
            Guid? tenantId = null,
            string? metadata = null
        ) : base(id, machineName, name, EavAttributeType.Serial, description, isRequired, tenantId, metadata)
        {
            Apply(new SerialAttributeConfigurationCreated(id, startingNumber, increment));
        }

        public override void UpdateAttribute(AttributeConfiguration updatedAttribute)
        {
            var updated = updatedAttribute as SerialAttributeConfiguration;

            if (updated == null)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            base.UpdateAttribute(updatedAttribute);

            if (Increment != updated.Increment)
            {
                Apply(
                    new SerialAttributeConfigurationUpdated(Id, updated.Increment)
                );
            }
        }

        public override bool Equals(object obj)
        {
            var serialAttribute = obj as SerialAttributeConfiguration;

            if (serialAttribute == null)
            {
                return false;
            }

            return this.Equals(serialAttribute);
        }

        private bool Equals(SerialAttributeConfiguration other)
        {
            return base.Equals(other)
                && StartingNumber == other.StartingNumber
                && Increment == other.Increment;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartingNumber, Increment, (int)ValueType);
        }

        public void On(SerialAttributeConfigurationCreated @event)
        {
            StartingNumber = @event.StartingNumber;
            Increment = @event.Increment;
        }

        public void On(SerialAttributeConfigurationUpdated @event)
        {
            Increment = @event.Increment;
        }
    }
}

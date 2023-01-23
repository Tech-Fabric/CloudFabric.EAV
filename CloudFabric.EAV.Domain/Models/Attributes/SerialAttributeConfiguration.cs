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
            Guid? TenantId = null,
            string? metadata = null
        ) : base(id, machineName, name, EavAttributeType.Serial, description, isRequired, TenantId, metadata)
        {
            Apply(new SerialAttributeConfigurationUpdate(id, startingNumber, increment));
        }

        public override void UpdateAttribute(AttributeConfiguration updatedAttribute)
        {
            var updated = updatedAttribute as SerialAttributeConfiguration;

            if (updated == null)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            base.UpdateAttribute(updatedAttribute);

            if (StartingNumber != updated.StartingNumber
                || Increment != updated.Increment)
            {
                Apply(
                    new SerialAttributeConfigurationUpdate(Id, updated.StartingNumber, updated.Increment)
                );
            }
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as SerialAttributeConfiguration);
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

        public void On(SerialAttributeConfigurationUpdate @event)
        {
            StartingNumber = @event.StartingNumber;
            Increment = @event.Increment;
        }
    }
}

using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class DateRangeAttributeConfiguration : AttributeConfiguration

    {
        public DateRangeAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {
        }

        public DateRangeAttributeConfiguration(Guid id,
            string machineName,
            List<LocalizedString> name,
            EavAttributeType valueType,
            DateRangeAttributeType dateRangeAttributeType,
            List<LocalizedString> description = null,
            bool isRequired = false,
            Guid? TenantId = null) : base(id, machineName, name, valueType, description, isRequired, TenantId)
        {
            Apply(new DateRangeAttributeConfigurationUpdated(id, dateRangeAttributeType));
        }

        public DateRangeAttributeType DateRangeAttributeType { get; set; }

        public override EavAttributeType ValueType => EavAttributeType.DateRange;

        public override List<string> ValidateInstance(AttributeInstance? instance)
        {
            List<string> errors = base.ValidateInstance(instance);

            if (instance == null)
            {
                return errors;
            }

            if (instance is not DateRangeAttributeInstance)
            {
                errors.Add("Cannot validate attribute. Expected attribute type: DateRange)");
                return errors;
            }
            return errors;
        }

        public override void UpdateAttribute(AttributeConfiguration updatedAttribute)
        {
            var updated = updatedAttribute as DateRangeAttributeConfiguration;

            if (updated == null)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            base.UpdateAttribute(updatedAttribute);

            if (DateRangeAttributeType != updated.DateRangeAttributeType)
            {
                Apply(new DateRangeAttributeConfigurationUpdated(Id, updated.DateRangeAttributeType));
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DateRangeAttributeConfiguration);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), DateRangeAttributeType);
        }

        private bool Equals(DateRangeAttributeConfiguration other)
        {
            return base.Equals(other)
                   && DateRangeAttributeType.Equals(other.DateRangeAttributeType)
                   && ValueType == other.ValueType;
        }

        #region EventHandlers

        public void On(DateRangeAttributeConfigurationUpdated @event)
        {
            DateRangeAttributeType = @event.DateRangeAttributeType;
        }

        #endregion
    }
}
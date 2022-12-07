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
            bool isSingleDate,
            List<LocalizedString> description = null,
            bool isRequired = false,
            Guid? TenantId = null) : base(id, machineName, name, valueType, description, isRequired, TenantId)
        {
            Apply(new DateRangeAttributeConfigurationUpdated(isSingleDate));
        }
        public bool IsSingleDate { get; set; }
        public override EavAttributeType ValueType => EavAttributeType.DateRange;

        public override List<string> Validate(AttributeInstance? instance)
        {
            List<string> errors = base.Validate(instance);

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

        public override bool Equals(object obj)
        {
            return Equals(obj as DateRangeAttributeConfiguration);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), IsSingleDate);
        }

        private bool Equals(DateRangeAttributeConfiguration other)
        {
            return base.Equals(other)
                   && IsSingleDate.Equals(other.IsSingleDate)
                   && ValueType == other.ValueType;
        }

        #region EventHandlers

        public void On(DateRangeAttributeConfigurationUpdated @event)
        {
            IsSingleDate = @event.IsSingleDate;
        }

        #endregion
    }
}
using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class ArrayAttributeConfiguration : AttributeConfiguration
    {
        public override EavAttributeType ValueType { get; } = EavAttributeType.Array;

        public EavAttributeType ItemsType { get; protected set; }

        public Guid ItemsAttributeConfigurationId { get; protected set; }

        public ArrayAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {
        }

        public ArrayAttributeConfiguration(
            Guid id,
            string machineName,
            List<LocalizedString> name,
            EavAttributeType itemsType,
            Guid itemsAttributeConfigurationId,
            List<LocalizedString> description = null,
            bool isRequired = false,
            Guid? tenantId = null,
            string? metadata = null
        ) : base(id, machineName, name, EavAttributeType.Array, description, isRequired, tenantId, metadata)
        {
            Apply(new ArrayAttributeConfigurationUpdated(id, itemsType, itemsAttributeConfigurationId));
        }

        public override List<string> Validate()
        {
            var errors = base.Validate();
            if (!Enum.IsDefined(typeof(EavAttributeType), ItemsType))
            {
                errors.Add("Unknown value type");
            }
            return errors;
        }

        public override void UpdateAttribute(AttributeConfiguration updatedAttribute)
        {
            var updated = updatedAttribute as ArrayAttributeConfiguration;

            if (updated == null)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            base.UpdateAttribute(updatedAttribute);

            if (ItemsType != updated.ItemsType || ItemsAttributeConfigurationId != updated.ItemsAttributeConfigurationId)
            {
                Apply(new ArrayAttributeConfigurationUpdated(Id, updated.ItemsType, updated.ItemsAttributeConfigurationId));
            }
        }

        #region EventHandlers

        public void On(ArrayAttributeConfigurationUpdated @event)
        {
            ItemsType = @event.ItemsType;
            ItemsAttributeConfigurationId = @event.ItemsAttributeConfigurationId;
        }

        #endregion
    }
}

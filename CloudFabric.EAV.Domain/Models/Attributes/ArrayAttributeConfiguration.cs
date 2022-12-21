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
            Guid? tenantId = null
        ) : base(id, machineName, name, EavAttributeType.Array, description, isRequired, tenantId)
        {
            Update(itemsType, itemsAttributeConfigurationId);
        }

        public void Update(EavAttributeType newItemsType, Guid newItemsAttributeConfiguration)
        {
            Apply(new ArrayAttributeConfigurationUpdated(newItemsType, newItemsAttributeConfiguration));
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
using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class LocalizedTextAttributeConfiguration : AttributeConfiguration
    {
        public LocalizedString DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.LocalizedText;

        public LocalizedTextAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {
        }

        public LocalizedTextAttributeConfiguration(
            Guid id,
            string machineName,
            List<LocalizedString> name,
            LocalizedString defaultValue,
            List<LocalizedString> description = null,
            bool isRequired = false
        ) : base(id, machineName, name, EavAttributeType.LocalizedText, description, isRequired)
        {
            Update(defaultValue);
        }

        public void Update(LocalizedString defaultValue)
        {
            Apply(new LocalizedTextAttributeConfigurationUpdated(defaultValue));
        }

        #region EventHandlers

        public void On(LocalizedTextAttributeConfigurationUpdated @event)
        {
            DefaultValue = @event.DefaultValue;
        }

        #endregion
    }
}
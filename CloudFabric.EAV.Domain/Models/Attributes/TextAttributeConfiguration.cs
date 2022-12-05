using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class TextAttributeConfiguration : AttributeConfiguration
    {
        public string DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Text;
        
        public TextAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {
            
        }
        
        public TextAttributeConfiguration(
            Guid id, 
            string machineName, 
            List<LocalizedString> name,
            string defaultValue,
            List<LocalizedString> description = null, 
            bool isRequired = false,
            Guid? tenantId = null
        ) : base(id, machineName, name, EavAttributeType.Text, description, isRequired, tenantId) {
            Apply(new TextAttributeConfigurationUpdated(defaultValue));
        }
        
        #region EventHandlers

        public void On(TextAttributeConfigurationUpdated @event)
        {
            DefaultValue = @event.DefaultValue;
        }

        #endregion
    }
}
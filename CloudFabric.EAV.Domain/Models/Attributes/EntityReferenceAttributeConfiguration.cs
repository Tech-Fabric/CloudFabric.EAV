using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class EntityReferenceAttributeConfiguration : AttributeConfiguration
    {
        public override EavAttributeType ValueType { get; } = EavAttributeType.EntityReference;

        public Guid ReferenceEntityConfiguration { get; set; }

        public Guid DefaultValue { get; set; }
        
        public EntityReferenceAttributeConfiguration(
            Guid id, 
            string machineName, 
            List<LocalizedString> name,
            Guid referenceEntityConfiguration,
            Guid defaultValue,
            List<LocalizedString> description = null, 
            bool isRequired = false
        ) : base(id, machineName, name, EavAttributeType.EntityReference, description, isRequired) {
            Update(referenceEntityConfiguration, defaultValue);
        }
        
        public void Update(Guid referenceEntityConfiguration, Guid defaultValue)
        {
            Apply(new EntityReferenceAttributeConfigurationUpdated(referenceEntityConfiguration, defaultValue));
        }
        
        #region EventHandlers
        public void On(EntityReferenceAttributeConfigurationUpdated @event)
        {
            ReferenceEntityConfiguration = @event.ReferenceEntityConfiguration;
            DefaultValue = @event.DefaultValue;
        }
        #endregion
    }
}
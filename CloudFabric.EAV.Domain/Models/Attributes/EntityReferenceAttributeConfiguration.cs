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
            bool isRequired = false,
            Guid? tenantId = null,
            string? metadata = null
        ) : base(id, machineName, name, EavAttributeType.EntityReference, description, isRequired, tenantId, metadata)
        {
            Apply(new EntityReferenceAttributeConfigurationUpdated(id, referenceEntityConfiguration, defaultValue));
        }

        public override void UpdateAttribute(AttributeConfiguration updatedAttribute)
        {
            var updated = updatedAttribute as EntityReferenceAttributeConfiguration;

            if (updated == null)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            base.UpdateAttribute(updatedAttribute);

            if (ReferenceEntityConfiguration != updated.ReferenceEntityConfiguration || DefaultValue != updated.DefaultValue)
            {
                Apply(new EntityReferenceAttributeConfigurationUpdated(Id, updated.ReferenceEntityConfiguration, updated.DefaultValue));
            }
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

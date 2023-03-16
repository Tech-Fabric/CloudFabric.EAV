using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes;

public class LocalizedTextAttributeConfiguration : AttributeConfiguration
{
    public LocalizedTextAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
    {
    }

    public LocalizedTextAttributeConfiguration(
        Guid id,
        string machineName,
        List<LocalizedString> name,
        LocalizedString defaultValue,
        List<LocalizedString> description = null,
        bool isRequired = false,
        Guid? tenantId = null,
        string? metadata = null
    ) : base(id, machineName, name, EavAttributeType.LocalizedText, description, isRequired, tenantId, metadata)
    {
        Apply(new LocalizedTextAttributeConfigurationUpdated(id, defaultValue));
    }

    public LocalizedString DefaultValue { get; set; }

    public override EavAttributeType ValueType { get; } = EavAttributeType.LocalizedText;

    public override void UpdateAttribute(AttributeConfiguration updatedAttribute)
    {
        var updated = updatedAttribute as LocalizedTextAttributeConfiguration;

        if (updated == null)
        {
            throw new ArgumentException("Invalid attribute type");
        }

        base.UpdateAttribute(updatedAttribute);

        if (DefaultValue != updated.DefaultValue)
        {
            Apply(new LocalizedTextAttributeConfigurationUpdated(Id, updated.DefaultValue));
        }
    }

    #region EventHandlers

    public void On(LocalizedTextAttributeConfigurationUpdated @event)
    {
        DefaultValue = @event.DefaultValue;
    }

    #endregion
}

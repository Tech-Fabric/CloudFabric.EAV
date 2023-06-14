using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes;

public class LocalizedTextAttributeConfiguration : AttributeConfiguration
{
    public LocalizedString? DefaultValue { get; set; }

    public override EavAttributeType ValueType { get; } = EavAttributeType.LocalizedText;

    #region Init
    public LocalizedTextAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
    {
    }

    public LocalizedTextAttributeConfiguration(
        Guid id,
        string machineName,
        List<LocalizedString> name,
        List<LocalizedString>? description = null,
        LocalizedString? defaultValue = null,
        bool isRequired = false,
        Guid? tenantId = null,
        string? metadata = null
    ) : base(id, machineName, name, EavAttributeType.LocalizedText, description, isRequired, tenantId, metadata)
    {
        Apply(new LocalizedTextAttributeConfigurationUpdated(id, defaultValue));
    }

    public LocalizedTextAttributeConfiguration(string machineName, Guid? tenantId) : this(Guid.NewGuid(),
        machineName,
        new List<LocalizedString>
        {
            LocalizedString.English("Localized string")
        },
        new List<LocalizedString>
        {
            LocalizedString.English("Localized string")
        },
        tenantId: tenantId)
    {

    }
    #endregion


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

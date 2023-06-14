using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record LocalizedTextAttributeConfigurationUpdated : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public LocalizedTextAttributeConfigurationUpdated()
    {
    }

    public LocalizedTextAttributeConfigurationUpdated(Guid id, LocalizedString? defaultValue)
    {
        AggregateId = id;
        DefaultValue = defaultValue;
    }

    public LocalizedString? DefaultValue { get; set; }
}

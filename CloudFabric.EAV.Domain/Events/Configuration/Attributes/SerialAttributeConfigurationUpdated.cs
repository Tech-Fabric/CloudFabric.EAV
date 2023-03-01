using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record SerialAttributeConfigurationUpdated : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public SerialAttributeConfigurationUpdated()
    {
    }

    public SerialAttributeConfigurationUpdated(Guid id, int increment)
    {
        AggregateId = id;
        Increment = increment;
    }

    public int Increment { get; set; }
}

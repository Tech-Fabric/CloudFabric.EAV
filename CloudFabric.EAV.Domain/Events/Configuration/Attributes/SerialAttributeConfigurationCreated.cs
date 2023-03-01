using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record SerialAttributeConfigurationCreated : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public SerialAttributeConfigurationCreated()
    {
    }

    public SerialAttributeConfigurationCreated(Guid id, long startingNumber, int increment)
    {
        AggregateId = id;
        StartingNumber = startingNumber;
        Increment = increment;
    }

    public long StartingNumber { get; set; }

    public int Increment { get; set; }
}

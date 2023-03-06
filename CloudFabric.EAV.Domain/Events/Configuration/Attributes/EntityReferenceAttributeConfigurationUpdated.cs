using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record EntityReferenceAttributeConfigurationUpdated : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public EntityReferenceAttributeConfigurationUpdated()
    {
    }

    public EntityReferenceAttributeConfigurationUpdated(Guid id, Guid referenceEntityConfiguration,
        Guid defaultValue)
    {
        AggregateId = id;
        DefaultValue = defaultValue;
        ReferenceEntityConfiguration = referenceEntityConfiguration;
    }

    public Guid ReferenceEntityConfiguration { get; set; }

    public Guid DefaultValue { get; set; }
}

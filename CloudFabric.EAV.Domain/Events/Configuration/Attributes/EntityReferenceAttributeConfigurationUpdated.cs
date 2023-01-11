using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record EntityReferenceAttributeConfigurationUpdated : Event
{
    public EntityReferenceAttributeConfigurationUpdated()
    {
    }

    public EntityReferenceAttributeConfigurationUpdated(Guid id, Guid referenceEntityConfiguration, Guid defaultValue)
    {
        AggregateId = id;
        DefaultValue = defaultValue;
        ReferenceEntityConfiguration = referenceEntityConfiguration;
    }

    public Guid ReferenceEntityConfiguration { get; set; }

    public Guid DefaultValue { get; set; }
}

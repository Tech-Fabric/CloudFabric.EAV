using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity;

public record EntityConfigurationAttributeUpdated : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public EntityConfigurationAttributeUpdated()
    {
    }

    public EntityConfigurationAttributeUpdated(Guid entityConfigurationId, AttributeConfiguration attribute)
    {
        AggregateId = entityConfigurationId;
        Attribute = attribute;
    }

    public AttributeConfiguration Attribute { get; set; }
}

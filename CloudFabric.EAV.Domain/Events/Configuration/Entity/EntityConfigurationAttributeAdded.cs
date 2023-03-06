using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity;

public record EntityConfigurationAttributeAdded : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public EntityConfigurationAttributeAdded()
    {
    }

    public EntityConfigurationAttributeAdded(Guid entityConfigurationId,
        EntityConfigurationAttributeReference attributeReference)
    {
        AggregateId = entityConfigurationId;
        AttributeReference = attributeReference;
    }

    public EntityConfigurationAttributeReference AttributeReference { get; set; }
}

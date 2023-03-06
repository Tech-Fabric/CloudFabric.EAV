using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record ArrayAttributeConfigurationUpdated : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public ArrayAttributeConfigurationUpdated()
    {
    }

    public ArrayAttributeConfigurationUpdated(Guid id, EavAttributeType itemsType,
        Guid itemsAttributeConfigurationId)
    {
        AggregateId = id;
        ItemsAttributeConfigurationId = itemsAttributeConfigurationId;
        ItemsType = itemsType;
    }

    public EavAttributeType ItemsType { get; set; }

    public Guid ItemsAttributeConfigurationId { get; set; }
}

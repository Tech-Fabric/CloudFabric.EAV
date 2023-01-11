using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record ArrayAttributeConfigurationUpdated : Event
{
    public ArrayAttributeConfigurationUpdated()
    {
    }

    public ArrayAttributeConfigurationUpdated(Guid id, EavAttributeType itemsType, Guid itemsAttributeConfigurationId)
    {
        AggregateId = id;
        ItemsAttributeConfigurationId = itemsAttributeConfigurationId;
        ItemsType = itemsType;
    }

    public EavAttributeType ItemsType { get; set; }

    public Guid ItemsAttributeConfigurationId { get; set; }
}

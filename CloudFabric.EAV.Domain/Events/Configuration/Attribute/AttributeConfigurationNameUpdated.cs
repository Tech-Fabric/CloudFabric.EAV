using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attribute;

public record AttributeConfigurationNameUpdated : Event
{
    public AttributeConfigurationNameUpdated()
    {
    }

    public AttributeConfigurationNameUpdated(Guid id, string newName, int cultureInfoId)
    {
        AggregateId = id;
        NewName = newName;
        CultureInfoId = cultureInfoId;
    }

    public string NewName { get; set; }

    public int CultureInfoId { get; set; }
}

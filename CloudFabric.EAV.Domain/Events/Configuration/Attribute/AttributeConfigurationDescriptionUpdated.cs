using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attribute;

public record AttributeConfigurationDescriptionUpdated : Event
{
    public AttributeConfigurationDescriptionUpdated(Guid id, string newDescription, int cultureInfoId)
    {
        AggregateId = id;
        NewDescription = newDescription;
        CultureInfoId = cultureInfoId;
    }

    public string NewDescription { get; set; }

    public int CultureInfoId { get; set; }
}

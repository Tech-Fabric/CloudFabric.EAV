using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity;

public record EntityConfigurationNameUpdated : Event
{
    public EntityConfigurationNameUpdated()
    {
    }

    public EntityConfigurationNameUpdated(Guid id, string newName, int cultureInfoId)
    {
        AggregateId = id;
        CultureInfoId = cultureInfoId;
        NewName = newName;
    }

    public string NewName { get; set; }

    public int CultureInfoId { get; set; }
}

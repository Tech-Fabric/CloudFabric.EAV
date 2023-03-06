using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity;

public record EntityConfigurationNameUpdated : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
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

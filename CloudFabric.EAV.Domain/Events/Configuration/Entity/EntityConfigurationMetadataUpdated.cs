using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity
{
    public record EntityConfigurationMetadataUpdated(Guid Id, Dictionary<string, object> Metadata) : Event;
}
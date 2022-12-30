using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity
{
    public record EntityConfigurationMetadataUpdated : Event
    {
        public EntityConfigurationMetadataUpdated()
        {
        }

        public EntityConfigurationMetadataUpdated(Guid id, Dictionary<string, object> metadata)
        {
            AggregateId = id;
            Metadata = metadata;
        }

        public Dictionary<string, object> Metadata { get; set; }
    }
}
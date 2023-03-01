using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes
{
    public record FileAttributeConfigurationUpdated : Event
    {
        // ReSharper disable once UnusedMember.Global
        // This constructor is required for Event Store to properly deserialize from json
        public FileAttributeConfigurationUpdated()
        {
        }

        public FileAttributeConfigurationUpdated(Guid id, bool isDownloadable)
        {
            AggregateId = id;
            IsDownloadable = isDownloadable;
        }

        public bool IsDownloadable { get; set; }
    }
}

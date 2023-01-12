using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes
{
    public record FileAttributeConfigurationUpdated : Event
    {
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
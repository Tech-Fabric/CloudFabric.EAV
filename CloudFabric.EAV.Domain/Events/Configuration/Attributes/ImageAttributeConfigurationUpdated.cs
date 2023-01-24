using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record ImageAttributeConfigurationUpdated : Event
{
    public ImageAttributeConfigurationUpdated()
    {
    }

    public ImageAttributeConfigurationUpdated(Guid id, List<ImageThumbnailDefinition> thumbnailsConfiguration)
    {
        AggregateId = id;
        ThumbnailsConfiguration = thumbnailsConfiguration;
    }

    public List<ImageThumbnailDefinition> ThumbnailsConfiguration { get; set; }
}
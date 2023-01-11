using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record ImageAttributeConfigurationUpdated : Event
{
    public ImageAttributeConfigurationUpdated()
    {
    }

    public ImageAttributeConfigurationUpdated(Guid id, ImageAttributeValue defaultValue, List<ImageThumbnailDefinition> thumbnailsConfiguration)
    {
        AggregateId = id;
        ThumbnailsConfiguration = thumbnailsConfiguration;
        DefaultValue = defaultValue;
    }

    public ImageAttributeValue DefaultValue { get; set; }

    public List<ImageThumbnailDefinition> ThumbnailsConfiguration { get; set; }
}
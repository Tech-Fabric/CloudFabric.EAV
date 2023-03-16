using System.Collections.Immutable;

using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record ImageAttributeConfigurationUpdated : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public ImageAttributeConfigurationUpdated()
    {
    }

    public ImageAttributeConfigurationUpdated(Guid id, ImmutableList<ImageThumbnailDefinition> thumbnailsConfiguration)
    {
        AggregateId = id;
        ThumbnailsConfiguration = thumbnailsConfiguration;
    }

    public ImmutableList<ImageThumbnailDefinition> ThumbnailsConfiguration { get; set; }
}

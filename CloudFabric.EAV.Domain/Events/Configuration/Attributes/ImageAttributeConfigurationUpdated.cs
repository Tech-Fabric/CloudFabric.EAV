using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record ImageAttributeConfigurationUpdated(ImageAttributeValue DefaultValue, List<ImageThumbnailDefinition> ThumbnailsConfiguration) : Event;
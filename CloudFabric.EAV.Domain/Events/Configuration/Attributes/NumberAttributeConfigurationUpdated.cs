using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record NumberAttributeConfigurationUpdated(float DefaultValue, float? MinimumValue, float? MaximumValue) : Event;
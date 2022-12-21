using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record NumberAttributeConfigurationUpdated(decimal DefaultValue, decimal? MinimumValue, decimal? MaximumValue, NumberAttributeType NumberType) : Event;
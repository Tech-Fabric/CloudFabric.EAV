using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attribute;

public record AttributeConfigurationIsRequiredFlagUpdated(Guid Id, bool NewIsRequired) : Event;
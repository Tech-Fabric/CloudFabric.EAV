using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record EntityReferenceAttributeConfigurationUpdated(Guid ReferenceEntityConfiguration, Guid DefaultValue) : Event;
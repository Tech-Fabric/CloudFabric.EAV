using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attribute;

public record AttributeConfigurationDescriptionUpdated(Guid Id, string NewDescription, int CultureInfoId) : Event;
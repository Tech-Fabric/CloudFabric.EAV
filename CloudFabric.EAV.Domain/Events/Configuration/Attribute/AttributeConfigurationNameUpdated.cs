using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attribute;

public record AttributeConfigurationNameUpdated(Guid Id, string NewName, int CultureInfoId) : Event;
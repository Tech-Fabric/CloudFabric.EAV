using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Attribute;

public record AttributeInstanceCreated(string configurationAttributeMachineName) : Event;

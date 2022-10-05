using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Events.Instance.Attribute;

public record AttributeInstanceCreated(string configurationAttributeMachineName) : Event;

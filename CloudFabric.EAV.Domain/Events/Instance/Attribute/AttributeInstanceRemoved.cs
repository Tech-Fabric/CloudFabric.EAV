using System;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity;

public record AttributeInstanceRemoved(Guid EntityInstanceId, string AttributeMachineName) : Event;

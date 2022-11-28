using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity;

public record EntityInstanceCreated(Guid Id, Guid EntityConfigurationId, string CategoryPath, List<AttributeInstance> Attributes) : Event;
using System;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity;

public record AttributeInstanceUpdated(Guid EntityInstanceId, AttributeInstance AttributeInstance) : Event;

using System;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity;

public record AttributeInstanceAdded(Guid EntityInstanceId, AttributeInstance AttributeInstance) : Event;

using System;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Events.Instance.Entity;

public record RemoveAttributeInstance(Guid Id) : Event;

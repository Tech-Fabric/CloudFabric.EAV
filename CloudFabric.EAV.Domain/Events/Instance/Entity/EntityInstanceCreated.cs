using System;
using System.Collections.Generic;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Instance.Entity;

public record EntityInstanceCreated(Guid Id, Guid EntityConfigurationId, List<AttributeInstance> Attributes) : Event;

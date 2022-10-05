using System;
using System.Collections.Generic;
using CloudFabric.EAV.Data.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Events.Instance.Entity;

public record EntityInstanceCreated(Guid EntityConfigurationId, EntityConfiguration EntityConfiguration, List<AttributeInstance> Attributes) : Event;

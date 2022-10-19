using System;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity;

public record EntityConfigurationAttributeRemoved(Guid EntityConfigurationId, string AttributeMachineName) : Event;
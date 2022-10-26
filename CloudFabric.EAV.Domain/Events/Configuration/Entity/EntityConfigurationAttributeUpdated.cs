using System;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity;

public record EntityConfigurationAttributeUpdated(Guid EntityConfigurationId, AttributeConfiguration Attribute) : Event;

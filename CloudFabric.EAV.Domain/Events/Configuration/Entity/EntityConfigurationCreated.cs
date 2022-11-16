using System;
using System.Collections.Generic;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity;

public record EntityConfigurationCreated(Guid Id, List<LocalizedString> Name, string MachineName, List<AttributeConfiguration> Attributes, Guid? TenantId, Dictionary<string, object> Metadata) : Event;

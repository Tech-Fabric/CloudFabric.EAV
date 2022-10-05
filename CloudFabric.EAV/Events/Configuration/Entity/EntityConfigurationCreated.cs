using System.Collections.Generic;
using CloudFabric.EAV.Data.Models;
using CloudFabric.EAV.Data.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Events.Configuration.Entity;

public record EntityConfigurationCreated(List<LocalizedString> Name, string MachineName, List<AttributeConfiguration> Attributes) : Event;

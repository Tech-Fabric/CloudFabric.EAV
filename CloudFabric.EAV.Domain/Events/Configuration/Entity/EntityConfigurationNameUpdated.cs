using System;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity;

public record EntityConfigurationNameUpdated(Guid Id, string NewName, int CultureInfoId) : Event;

using System;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Events.Configuration.Entity;

public record RemoveAttributeConfiguration(Guid Id) : Event;

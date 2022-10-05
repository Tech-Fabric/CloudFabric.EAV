using System;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Events.Configuration.Attributes;

public record MachineNameChanged(Guid attributeId, string NewMachineName) : Event;

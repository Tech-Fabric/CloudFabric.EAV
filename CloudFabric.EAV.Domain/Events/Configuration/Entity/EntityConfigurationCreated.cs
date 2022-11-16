using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity;

public record EntityConfigurationCreated(Guid Id, List<LocalizedString> Name, string MachineName, List<EntityConfigurationAttributeReference> Attributes) : Event;

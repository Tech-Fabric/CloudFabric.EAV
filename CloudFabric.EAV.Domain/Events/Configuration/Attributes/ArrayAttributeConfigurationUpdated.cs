using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record ArrayAttributeConfigurationUpdated(EavAttributeType ItemsType, Guid ItemsAttributeConfigurationId) : Event;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record LocalizedTextAttributeConfigurationUpdated(LocalizedString DefaultValue) : Event;
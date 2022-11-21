using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attribute;

public record AttributeConfigurationCreated(Guid Id, string MachineName, List<LocalizedString> Name, EavAttributeType ValueType, List<LocalizedString> Description, bool IsRequired) : Event;
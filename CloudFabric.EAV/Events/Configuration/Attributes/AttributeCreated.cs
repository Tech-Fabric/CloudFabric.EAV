using System.Collections.Generic;
using CloudFabric.EAV.Data.Models;
using CloudFabric.EAV.Data.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Events.Configuration.Attributes;

public record AttributeCreated(List<LocalizedString> Name, List<LocalizedString> Description, string MachineName, List<AttributeValidationRule> ValidationRules) : Event;

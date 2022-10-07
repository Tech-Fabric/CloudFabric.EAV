using System.Collections.Generic;
using CloudFabric.EAV.Data.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Events.Configuration.Entity;

public record ChangeEntityConfigName(List<LocalizedString> Name): Event;

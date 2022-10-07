using CloudFabric.EAV.Data.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Events.Instance.Entity;

public record AddAttributeInstance(AttributeInstance AttributeInstance) : Event;

using CloudFabric.EAV.Data.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Events.Configuration.Entity;

public record AddAttributeConfiguration(AttributeConfiguration Configuration): Event;

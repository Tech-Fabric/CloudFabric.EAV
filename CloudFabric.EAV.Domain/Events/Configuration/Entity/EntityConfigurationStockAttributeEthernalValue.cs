using CloudFabric.EAV.Domain.Models;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity
{
    public record EntityConfigurationAttributeOverridesUpdated : Event
    {
        public EntityConfigurationAttributeOverridesUpdated()
        {
        }

        public EntityConfigurationAttributeOverridesUpdated(Guid entityId, Guid attributeConfigurationId, List<object> overrides)
        {
            AggregateId = entityId;
            AttributeConfigurationId = attributeConfigurationId;
            Overrides = overrides;
        }

        public Guid AttributeConfigurationId { get; set; }
        public List<object> Overrides { get; set; }
    }
}

using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Entity
{
    public record EntityConfigurationAttributeExternalValuesUpdated : Event
    {
        // ReSharper disable once UnusedMember.Global
        // This constructor is required for Event Store to properly deserialize from json
        public EntityConfigurationAttributeExternalValuesUpdated()
        {
        }

        public EntityConfigurationAttributeExternalValuesUpdated(
            Guid entityId, Guid attributeConfigurationId, List<object> values
        )
        {
            AggregateId = entityId;
            AttributeConfigurationId = attributeConfigurationId;
            ExternalValues = values;
        }

        public Guid AttributeConfigurationId { get; set; }
        public List<object> ExternalValues { get; set; }
    }
}

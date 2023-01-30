using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes
{
    public record SerialAttributeConfigurationUpdated : Event
    {
        public SerialAttributeConfigurationUpdated()
        {

        }

        public SerialAttributeConfigurationUpdated(Guid id, int increment)
        {
            AggregateId = id;
            Increment = increment;
        }

        public int Increment { get; set; }
    }
}

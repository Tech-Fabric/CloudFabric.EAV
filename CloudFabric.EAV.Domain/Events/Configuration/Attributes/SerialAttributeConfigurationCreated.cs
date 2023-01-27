using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes
{
    public record SerialAttributeConfigurationCreated :Event
    {
        public SerialAttributeConfigurationCreated()
        {

        }

        public SerialAttributeConfigurationCreated(Guid id, long startingNumber, int increment)
        {
            AggregateId = id;
            StartingNumber = startingNumber;
            Increment = increment;
        }

        public long StartingNumber { get; set; }

        public int Increment { get; set; }
    }
}

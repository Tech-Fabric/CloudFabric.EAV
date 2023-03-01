using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes
{
    public record ValueFromListConfigurationUpdated : Event
    {
        // ReSharper disable once UnusedMember.Global
        // This constructor is required for Event Store to properly deserialize from json
        public ValueFromListConfigurationUpdated()
        {
        }

        public ValueFromListConfigurationUpdated(Guid id, List<ValueFromListOptionConfiguration> valueFromListOptions)
        {
            AggregateId = id;
            ValueFromListOptions = valueFromListOptions;
        }

        public List<ValueFromListOptionConfiguration> ValueFromListOptions { get; set; }
    }
}

using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes
{
    public record DateRangeAttributeConfigurationUpdated : Event
    {
        // ReSharper disable once UnusedMember.Global
        // This constructor is required for Event Store to properly deserialize from json
        public DateRangeAttributeConfigurationUpdated()
        {
        }

        public DateRangeAttributeConfigurationUpdated(Guid id, DateRangeAttributeType dateRangeAttributeType)
        {
            AggregateId = id;
            DateRangeAttributeType = dateRangeAttributeType;
        }

        public DateRangeAttributeType DateRangeAttributeType { get; set; }
    }
}

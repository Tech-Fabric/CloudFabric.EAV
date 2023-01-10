using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes
{
    public record DateRangeAttributeConfigurationUpdated : Event
    {
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
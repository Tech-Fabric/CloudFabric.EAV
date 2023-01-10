using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record NumberAttributeConfigurationUpdated : Event
{
    public NumberAttributeConfigurationUpdated()
    {
    }

    public NumberAttributeConfigurationUpdated(Guid id, decimal defaultValue, decimal? minimumValue, decimal? maximumValue, NumberAttributeType numberType)
    {
        AggregateId = id;
        NumberType = numberType;
        MaximumValue = maximumValue;
        MinimumValue = minimumValue;
        DefaultValue = defaultValue;
    }

    public decimal DefaultValue { get; set; }

    public decimal? MinimumValue { get; set; }

    public decimal? MaximumValue { get; set; }

    public NumberAttributeType NumberType { get; set; }
}

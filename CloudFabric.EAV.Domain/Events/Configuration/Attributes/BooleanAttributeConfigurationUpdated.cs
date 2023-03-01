using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record BooleanAttributeConfigurationUpdated : Event
{
    public BooleanAttributeConfigurationUpdated(Guid id, string trueDisplayValue, string falseDisplayValue)
    {
        AggregateId = id;
        FalseDisplayValue = falseDisplayValue;
        TrueDisplayValue = trueDisplayValue;
    }

    public string TrueDisplayValue { get; set; }

    public string FalseDisplayValue { get; set; }
}

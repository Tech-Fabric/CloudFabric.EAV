using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record BooleanAttributeConfigurationUpdated : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public BooleanAttributeConfigurationUpdated()
    {
    }

    public BooleanAttributeConfigurationUpdated(Guid id, string trueDisplayValue, string falseDisplayValue)
    {
        AggregateId = id;
        FalseDisplayValue = falseDisplayValue;
        TrueDisplayValue = trueDisplayValue;
    }

    public string TrueDisplayValue { get; set; }

    public string FalseDisplayValue { get; set; }
}

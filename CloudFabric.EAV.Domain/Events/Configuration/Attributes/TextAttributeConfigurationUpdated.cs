using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record TextAttributeConfigurationUpdated : Event
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public TextAttributeConfigurationUpdated()
    {
    }

    public TextAttributeConfigurationUpdated(Guid id, string? defaultValue, int? maxLength, bool isSearchable)
    {
        AggregateId = id;
        IsSearchable = isSearchable;
        MaxLength = maxLength;
        DefaultValue = defaultValue;
    }

    public string? DefaultValue { get; set; }

    public int? MaxLength { get; set; }

    public bool IsSearchable { get; set; }
}

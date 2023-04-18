using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes;

public record MoneyAttributeConfigurationUpdated : Event
{
    public MoneyAttributeConfigurationUpdated(Guid aggregateId, string defaultCurrencyId, List<Currency>? currencies)
    {
        DefaultCurrencyId = defaultCurrencyId;
        Currencies = currencies;
        AggregateId = aggregateId;
    }

    public MoneyAttributeConfigurationUpdated()
    {

    }
    public string DefaultCurrencyId { get; set; }
    public List<Currency>? Currencies { get; set; }

}

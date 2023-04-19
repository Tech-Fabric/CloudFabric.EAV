using System.Collections.ObjectModel;

using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Enums;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes;

public class MoneyAttributeConfiguration: AttributeConfiguration
{
    public override EavAttributeType ValueType => EavAttributeType.Money;
    public List<Currency> Currencies { get; set; }
    public string DefaultCurrencyId { get; set; }

    #region Init

    public MoneyAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
    {
    }

    public MoneyAttributeConfiguration(Guid id,
        string machineName,
        List<LocalizedString> name,
        string defaultCurrencyId,
        List<Currency>? currencies = null,
        List<LocalizedString>? description = null,
        bool isRequired = false,
        Guid? tenantId = null,
        string? metadata = null) : base(id, machineName, name, EavAttributeType.Money, description, isRequired, tenantId, metadata)
    {
        currencies ??= DefaultListOfCurrencies();
        Apply(new MoneyAttributeConfigurationUpdated(id, defaultCurrencyId, currencies));
    }

    public MoneyAttributeConfiguration(string machineName, Guid? tenantId) : this(Guid.NewGuid(),
        machineName,
        new List<LocalizedString>
        {
            LocalizedString.English("Price")
        },
        defaultCurrencyId: "usd",
        currencies: null,
        new List<LocalizedString>
        {
            LocalizedString.English("Price")
        },
        tenantId: tenantId)
    {

    }

    #endregion
    #region Validation

    public override List<string> Validate()
    {
        var errors = base.Validate();
        if (string.IsNullOrEmpty(DefaultCurrencyId))
        {
            errors.Add("Default currency cannot be empty");
        }
        var currencyListToValidate = (Currencies == null || Currencies.Count == 0) ? DefaultListOfCurrencies() : Currencies;
        if (!currencyListToValidate.Select(c => c.MachineName).Contains(DefaultCurrencyId))
        {
            errors.Add("Default currency must be one of the Currencies list");
        }
        return errors;
    }

    public override List<string> ValidateInstance(AttributeInstance? instance)
    {
        var errors = base.ValidateInstance(instance);
        if (instance is MoneyAttributeInstance)
        {
            return errors;
        }
        errors.Add("Cannot validate attribute. Expected attribute type: Money)");
        return errors;
    }

    #endregion
    #region Equality

    public override bool Equals(object obj)
    {
        return base.Equals(obj as MoneyAttributeConfiguration);
    }

    private bool Equals(MoneyAttributeConfiguration other)
    {
        return base.Equals(other)
               && DefaultCurrencyId.Equals(other.DefaultCurrencyId)
               && Currencies.Equals(other.Currencies);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), DefaultCurrencyId, Currencies.GetHashCode());
    }

    #endregion
    #region EventHandlers

    public void On(MoneyAttributeConfigurationUpdated @event)
    {
        DefaultCurrencyId = @event.DefaultCurrencyId;
        if (@event.Currencies is
            {
                Count: > 0
            })
        {
            Currencies = @event.Currencies;
        }
    }

    #endregion

    private List<Currency> DefaultListOfCurrencies() => new List<Currency>
    {
        new Currency("Euro", "eur", "€"),
        new Currency("US Dollar", "usd", "$")
    };
}

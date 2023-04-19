namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class MoneyAttributeConfigurationViewModel: AttributeConfigurationViewModel
{
    public List<CurrencyViewModel> Currencies { get; set; }
    public string DefaultCurrencyId { get; set; }
}

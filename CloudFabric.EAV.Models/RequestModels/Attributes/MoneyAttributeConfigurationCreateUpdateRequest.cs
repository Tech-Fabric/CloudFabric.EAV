using CloudFabric.EAV.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes;

public class MoneyAttributeConfigurationCreateUpdateRequest: AttributeConfigurationCreateUpdateRequest
{
    public override EavAttributeType ValueType => EavAttributeType.Money;
    public List<CurrencyRequestModel>? Currencies { get; set; }
    public string? DefaultCurrencyId { get; set; }
}

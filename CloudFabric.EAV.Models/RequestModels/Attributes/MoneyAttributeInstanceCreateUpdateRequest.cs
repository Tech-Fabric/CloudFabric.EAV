namespace CloudFabric.EAV.Models.RequestModels.Attributes;

public class MoneyAttributeInstanceCreateUpdateRequest: AttributeInstanceCreateUpdateRequest
{
    public decimal Value { get; set; }
}

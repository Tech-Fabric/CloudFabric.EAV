namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class MoneyAttributeInstanceViewModel: AttributeInstanceViewModel
{
    public decimal? Value { get; set; }
    public override object? GetValue()
    {
        throw new NotImplementedException();
    }
}

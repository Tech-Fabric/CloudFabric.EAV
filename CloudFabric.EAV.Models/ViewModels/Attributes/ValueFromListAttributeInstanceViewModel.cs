namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class ValueFromListAttributeInstanceViewModel : AttributeInstanceViewModel
{
    public string Value { get; set; }

    public override object? GetValue()
    {
        return Value;
    }
}

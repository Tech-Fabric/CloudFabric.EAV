namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class TextAttributeInstanceViewModel : AttributeInstanceViewModel
{
    public string Value { get; set; }

    public override object? GetValue()
    {
        return Value;
    }
}

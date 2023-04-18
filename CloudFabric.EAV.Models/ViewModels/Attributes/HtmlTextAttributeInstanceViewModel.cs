namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class HtmlTextAttributeInstanceViewModel : AttributeInstanceViewModel
{
    public string Value { get; set; }

    public override object? GetValue()
    {
        return Value;
    }
}

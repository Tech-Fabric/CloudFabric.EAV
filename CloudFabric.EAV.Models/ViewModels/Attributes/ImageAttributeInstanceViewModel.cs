namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class ImageAttributeInstanceViewModel : AttributeInstanceViewModel
{
    public ImageAttributeValueViewModel Value { get; set; }

    public override object? GetValue()
    {
        return Value;
    }
}

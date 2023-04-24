namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class BooleanAttributeInstanceViewModel : AttributeInstanceViewModel
{
    public bool Value { get; set; }

    public override object? GetValue()
    {
        return Value;
    }
}

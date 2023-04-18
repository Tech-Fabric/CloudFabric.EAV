namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class SerialAttributeInstanceViewModel : AttributeInstanceViewModel
{
    public long Value { get; set; }

    public override object? GetValue()
    {
        return Value;
    }
}

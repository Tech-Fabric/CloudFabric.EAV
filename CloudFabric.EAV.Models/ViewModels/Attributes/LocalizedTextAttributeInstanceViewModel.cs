namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class LocalizedTextAttributeInstanceViewModel : AttributeInstanceViewModel
{
    public List<LocalizedStringViewModel> Value { get; set; }
    public override object? GetValue()
    {
        return Value;
    }
}

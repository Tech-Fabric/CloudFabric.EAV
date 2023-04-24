namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class EntityReferenceAttributeInstanceViewModel : AttributeInstanceViewModel
{
    public Guid Value { get; set; }

    public override object? GetValue()
    {
        return Value;
    }
}

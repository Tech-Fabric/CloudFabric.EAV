namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class ArrayAttributeInstanceViewModel : AttributeInstanceViewModel
{
    public List<AttributeInstanceViewModel> Items { get; set; }
    public override object? GetValue()
    {
        return Items.Select(i => i.GetValue()).ToList();
    }
}

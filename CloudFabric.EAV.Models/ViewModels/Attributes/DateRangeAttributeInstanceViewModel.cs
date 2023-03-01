namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class DateRangeAttributeInstanceViewModel : AttributeInstanceViewModel
{
    public DateRangeAttributeInstanceValueViewModel Value { get; set; }
}

public class DateRangeAttributeInstanceValueViewModel
{
    public DateTime From { get; set; }
    public DateTime? To { get; set; }
}

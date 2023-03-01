using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class NumberAttributeConfigurationViewModel : AttributeConfigurationViewModel
{
    public decimal DefaultValue { get; set; }
    public decimal? MinimumValue { get; set; }
    public decimal? MaximumValue { get; set; }
    public NumberAttributeType NumberType { get; set; }
}

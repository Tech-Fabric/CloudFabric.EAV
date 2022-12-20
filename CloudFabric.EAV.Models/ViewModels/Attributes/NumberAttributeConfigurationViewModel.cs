using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class NumberAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public decimal DefaultValue { get; set; }
        public decimal? MinimumValue { get; set; }
        public decimal? MaximumValue { get; set; }
        public NumberAttributeType NumberType { get; set; }
    }
}
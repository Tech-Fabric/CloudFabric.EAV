using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class NumberAttributeInstanceViewModel : AttributeInstanceViewModel
    {
        public decimal Value { get; set; }
    }
}
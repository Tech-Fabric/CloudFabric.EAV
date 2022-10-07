using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class TextAttributeInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
    {
        public string Value { get; set; }
    }
}
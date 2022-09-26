using CloudFabric.EAV.Data.Enums;

namespace CloudFabric.EAV.Service.Models.RequestModels.Attributes
{
    public class TextAttributeInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
    {
        public string Value { get; set; }
    }
}
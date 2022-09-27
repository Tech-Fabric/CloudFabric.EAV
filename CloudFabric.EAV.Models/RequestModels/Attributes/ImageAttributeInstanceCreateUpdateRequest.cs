namespace CloudFabric.EAV.Service.Models.RequestModels.Attributes
{
    public class ImageAttributeInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
    {
        public ImageAttributeValueCreateUpdateRequest Value { get; set; }
    }
}
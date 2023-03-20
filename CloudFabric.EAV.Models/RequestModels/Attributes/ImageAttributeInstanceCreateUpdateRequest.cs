namespace CloudFabric.EAV.Models.RequestModels.Attributes;

public class ImageAttributeInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
{
    public ImageAttributeValueCreateUpdateRequest? Value { get; set; }
}

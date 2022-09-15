namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class ImageAttributeInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
    {
        public ImageAttributeValueCreateUpdateRequest Value { get; set; }
    }
}
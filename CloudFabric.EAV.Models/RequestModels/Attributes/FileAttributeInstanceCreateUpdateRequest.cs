namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class FileAttributeInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
    {
        public FileAttributeValueCreateUpdateRequest Value { get; set; }
    }

    public class FileAttributeValueCreateUpdateRequest
    {
        public string Url { get; set; }

        public string Filename { get; set; }
    }
}
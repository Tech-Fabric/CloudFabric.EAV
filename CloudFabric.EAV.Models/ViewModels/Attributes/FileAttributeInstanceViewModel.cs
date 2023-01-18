using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class FileAttributeInstanceViewModel : AttributeInstanceViewModel
    {
        public FileAttributeValueViewModel Value { get; set; }
    }

    public class FileAttributeValueViewModel
    {
        public string Url { get; set; }

        public string Filename { get; set; }
    }
}
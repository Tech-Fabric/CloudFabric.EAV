using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class FileAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public bool IsDownloadable { get; set; }
    }
}
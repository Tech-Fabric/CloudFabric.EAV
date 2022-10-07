using System.Collections.Generic;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class ImageThumbnailDefinitionViewModel
    {
        public int MaxWidth { get; set; }

        public int MaxHeight { get; set; }
    }

    public class ImageAttributeValueViewModel
    {
        public string Url { get; set; }
        public string Title { get; set; }

        public string Alt { get; set; }
    }

    public class ImageAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public ImageAttributeValueViewModel DefaultValue { get; set; }

        public List<ImageThumbnailDefinitionViewModel> ThumbnailsConfiguration { get; set; }
    }
}
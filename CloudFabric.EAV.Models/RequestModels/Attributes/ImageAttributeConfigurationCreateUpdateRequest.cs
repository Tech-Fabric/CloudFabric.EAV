using System.Collections.Generic;
using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class ImageThumbnailDefinitionCreateUpdateRequest
    {
        public int MaxWidth { get; set; }

        public int MaxHeight { get; set; }
    }

    public class ImageAttributeValueCreateUpdateRequest
    {
        public string Url { get; set; }
        public string Title { get; set; }

        public string Alt { get; set; }
    }

    public class ImageAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public ImageAttributeValueCreateUpdateRequest DefaultValue { get; set; }

        public List<ImageThumbnailDefinitionCreateUpdateRequest> ThumbnailsConfiguration { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Image;
    }
}
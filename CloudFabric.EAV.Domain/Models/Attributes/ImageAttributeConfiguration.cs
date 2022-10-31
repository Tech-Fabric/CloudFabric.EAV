using System.Collections.Generic;
using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class ImageThumbnailDefinition
    {
        public int MaxWidth { get; set; }

        public int MaxHeight { get; set; }
    }

    public class ImageAttributeValue
    {
        public string Url { get; set; }
        public string Title { get; set; }

        public string Alt { get; set; }
    }

    public class ImageAttributeConfiguration : AttributeConfiguration
    {
        public ImageAttributeValue DefaultValue { get; set; }

        public List<ImageThumbnailDefinition> ThumbnailsConfiguration { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Image;
        public override (bool, List<string>) Validate(AttributeInstance instance)
        {
            return (true, new List<string>());
        }
    }
}

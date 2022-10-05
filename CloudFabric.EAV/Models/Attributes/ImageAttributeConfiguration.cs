using CloudFabric.EAV.Data.Enums;
using System.Collections.Generic;
using CloudFabric.EAV.Data.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Models.Attributes
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

        public ImageAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {
        }

        public ImageAttributeConfiguration(List<LocalizedString> name, List<LocalizedString> description, string machineName, List<AttributeValidationRule> validationRules) : base(name, description, machineName, validationRules)
        {
        }
    }
}

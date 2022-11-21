﻿using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;

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
        
        public ImageAttributeConfiguration(
            Guid id, 
            string machineName, 
            List<LocalizedString> name,
            ImageAttributeValue defaultValue,
            List<ImageThumbnailDefinition> thumbnailsConfiguration = null,
            List<LocalizedString> description = null, 
            bool isRequired = false
        ) : base(id, machineName, name, EavAttributeType.Image, description, isRequired) {
            Apply(new ImageAttributeConfigurationUpdated(defaultValue, thumbnailsConfiguration));
        }
        
        #region EventHandlers
        public void On(ImageAttributeConfigurationUpdated @event)
        {
            DefaultValue = @event.DefaultValue;
            ThumbnailsConfiguration = @event.ThumbnailsConfiguration;
        }
        #endregion
    }
}

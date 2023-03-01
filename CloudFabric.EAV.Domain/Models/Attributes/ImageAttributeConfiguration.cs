using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;

namespace CloudFabric.EAV.Domain.Models.Attributes;

public class ImageThumbnailDefinition
{
    public static int MaxThumbnailSize = 1024;
    public int Width { get; set; }

    public int Height { get; set; }
    public string Name { get; set; }

    public override bool Equals(object obj)
    {
        return Equals(obj as ImageThumbnailDefinition);
    }

    private bool Equals(ImageThumbnailDefinition other)
    {
        return Width == other.Width
               && Height == other.Height
               && Name == other.Name;
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}

public class ImageAttributeValue
{
    public string Url { get; set; }
    public string Title { get; set; }

    public string Alt { get; set; }

    public override bool Equals(object obj)
    {
        return Equals(obj as ImageAttributeValue);
    }

    private bool Equals(ImageAttributeValue other)
    {
        return Url == other.Url
               && Title == other.Title
               && Alt == other.Alt;
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}

public class ImageAttributeConfiguration : AttributeConfiguration
{
    public ImageAttributeConfiguration(
        Guid id,
        string machineName,
        List<LocalizedString> name,
        List<ImageThumbnailDefinition> thumbnailsConfiguration = null,
        List<LocalizedString> description = null,
        bool isRequired = false,
        Guid? tenantId = null,
        string? metadata = null
    ) : base(id, machineName, name, EavAttributeType.Image, description, isRequired, tenantId, metadata)
    {
        Apply(new ImageAttributeConfigurationUpdated(id, thumbnailsConfiguration));
    }

    public List<ImageThumbnailDefinition> ThumbnailsConfiguration { get; set; }

    public override EavAttributeType ValueType => EavAttributeType.Image;

    public override void UpdateAttribute(AttributeConfiguration updatedAttribute)
    {
        var updated = updatedAttribute as ImageAttributeConfiguration;

        if (updated == null)
        {
            throw new ArgumentException("Invalid attribute type");
        }

        base.UpdateAttribute(updatedAttribute);

        if (!ThumbnailsConfiguration.Equals(updated.ThumbnailsConfiguration)
           )
        {
            Apply(new ImageAttributeConfigurationUpdated(Id, updated.ThumbnailsConfiguration));
        }
    }

    public override List<string> Validate()
    {
        List<string> errors = base.Validate();
        if (ThumbnailsConfiguration.Any(t => t.Height > ImageThumbnailDefinition.MaxThumbnailSize
                                             || t.Width > ImageThumbnailDefinition.MaxThumbnailSize
            ))
        {
            errors.Add(
                $"Thumbnail width or height cannot be greater than {ImageThumbnailDefinition.MaxThumbnailSize}"
            );
        }

        return errors;
    }

    public override List<string> ValidateInstance(AttributeInstance? instance)
    {
        List<string> errors = base.ValidateInstance(instance);

        if (instance == null)
        {
            return errors;
        }

        if (instance is not ImageAttributeInstance)
        {
            errors.Add("Cannot validate attribute. Expected attribute type: Image");
            return errors;
        }

        if (instance.GetValue() is not ImageAttributeValue instanceValue)
        {
            errors.Add("Cannot validate attribute. Expected attribute type: Image");
            return errors;
        }

        if (string.IsNullOrEmpty(instanceValue.Url))
        {
            errors.Add("Image URL cannot be empty");
        }

        if (string.IsNullOrEmpty(instanceValue.Alt))
        {
            errors.Add("Image Alt cannot be empty");
        }

        if (string.IsNullOrEmpty(instanceValue.Title))
        {
            errors.Add("Image Title cannot be empty");
        }

        return errors;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as ImageAttributeConfiguration);
    }

    private bool Equals(ImageAttributeConfiguration other)
    {
        return base.Equals(other)
               && ThumbnailsConfiguration.Equals(other.ThumbnailsConfiguration)
               && ValueType == other.ValueType;
    }


    #region EventHandlers

    public void On(ImageAttributeConfigurationUpdated @event)
    {
        ThumbnailsConfiguration = @event.ThumbnailsConfiguration;
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    #endregion
}

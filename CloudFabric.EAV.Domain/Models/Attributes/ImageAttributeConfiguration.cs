using System.Collections.Immutable;

using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Enums;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes;

public record ImageThumbnailDefinition
{
    public static int MaxThumbnailSize = 1024;
    public int MaxWidth { get; set; }

    public int MaxHeight { get; set; }

    public string Name { get; set; }
}

public record ImageAttributeValue
{
    public string Url { get; set; }
    public string Title { get; set; }

    public string Alt { get; set; }
}

public class ImageAttributeConfiguration : AttributeConfiguration, IEquatable<ImageAttributeConfiguration>
{
    public IReadOnlyCollection<ImageThumbnailDefinition> ThumbnailsConfiguration { get; set; }

    public override EavAttributeType ValueType => EavAttributeType.Image;
    public static new string DefaultMachineName => "image_default";

    #region Init
    public ImageAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
    {
    }

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
        Apply(new ImageAttributeConfigurationUpdated(id, thumbnailsConfiguration.ToImmutableList()));
    }

    public ImageAttributeConfiguration(string machineName, Guid? tenantId) : this(Guid.NewGuid(),
        machineName,
        new List<LocalizedString>
        {
            LocalizedString.English("Image")
        },
        new List<ImageThumbnailDefinition>()
        {
            new ImageThumbnailDefinition()
            {
                MaxHeight = 1000,
                MaxWidth = 1000,
                Name = "thumbnail"
            }
        },
        new List<LocalizedString>
        {
            LocalizedString.English("Image")
        },
        tenantId: tenantId)
    {

    }
    #endregion

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
            Apply(new ImageAttributeConfigurationUpdated(Id, updated.ThumbnailsConfiguration.ToImmutableList()));
        }
    }

    #region Validation
    public override List<string> Validate()
    {
        List<string> errors = base.Validate();
        if (ThumbnailsConfiguration.Any(t => t.MaxHeight > ImageThumbnailDefinition.MaxThumbnailSize
                                             || t.MaxWidth > ImageThumbnailDefinition.MaxThumbnailSize
            ))
        {
            errors.Add(
                $"Thumbnail width or height cannot be greater than {ImageThumbnailDefinition.MaxThumbnailSize}"
            );
        }

        return errors;
    }

    public override List<string> ValidateInstance(AttributeInstance? instance, bool requiredAttributesCanBeNull = false)
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
    #endregion

    #region Equality
    public override bool Equals(object obj)
    {
        return Equals(obj as ImageAttributeConfiguration);
    }

    public bool Equals(ImageAttributeConfiguration? other)
    {
        if (other is null)
        {
            return false;
        }

        // Optimization for a common success case.
        if (object.ReferenceEquals(this, other))
        {
            return true;
        }

        // If run-time types are not exactly the same, return false.
        if (this.GetType() != other.GetType())
        {
            return false;
        }

        return base.Equals(other)
               && ThumbnailsConfiguration.Equals(other.ThumbnailsConfiguration)
               && ValueType == other.ValueType;
    }
    #endregion

    #region EventHandlers

    public void On(ImageAttributeConfigurationUpdated @event)
    {
        ThumbnailsConfiguration = @event.ThumbnailsConfiguration.ToList().AsReadOnly();
    }

    public override int GetHashCode() => (ThumbnailsConfiguration, ValueType).GetHashCode();

    #endregion
}

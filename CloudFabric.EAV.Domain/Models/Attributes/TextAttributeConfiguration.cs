using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes;

public class TextAttributeConfiguration : AttributeConfiguration
{
    public string? DefaultValue { get; set; }

    public bool IsSearchable { get; set; }

    public int? MaxLength { get; set; }
    public override EavAttributeType ValueType => EavAttributeType.Text;

    #region Init
    public TextAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
    {
    }

    public TextAttributeConfiguration(
        Guid id,
        string machineName,
        List<LocalizedString> name,
        int? maxLength,
        bool isSearchable,
        List<LocalizedString>? description = null,
        string? defaultValue = null,
        bool isRequired = false,
        Guid? tenantId = null,
        string? metadata = null
    ) : base(id, machineName, name, EavAttributeType.Text, description, isRequired, tenantId, metadata)
    {
        Apply(new TextAttributeConfigurationUpdated(id, defaultValue, maxLength, isSearchable));
    }
    public TextAttributeConfiguration(string machineName, Guid? tenantId) : this(Guid.NewGuid(),
        machineName,
        new List<LocalizedString>
        {
            LocalizedString.English("Text attribute")
        },
        100,
        false,
        new List<LocalizedString>
        {
            LocalizedString.English("Text attribute")
        },
        tenantId: tenantId)
    {

    }
    #endregion

#region Validaton
    public override List<string> Validate()
    {
        List<string> errors = base.Validate();
        if (MaxLength != null && DefaultValue?.Length > MaxLength)
        {
            errors.Add("Default value length cannot be greater than MaxLength");
        }

        return errors;
    }

    public override List<string> ValidateInstance(AttributeInstance? instance, bool requiredAttributesCanBeNull = false)
    {
        List<string> errors = base.ValidateInstance(instance, requiredAttributesCanBeNull);

        if (instance == null)
        {
            return errors;
        }

        if (instance is not TextAttributeInstance textInstance)
        {
            errors.Add("Cannot validate attribute. Expected attribute type: Text");
            return errors;
        }

        if (IsRequired && string.IsNullOrWhiteSpace(textInstance.Value))
        {
            errors.Add("Required text value is missing");
            return errors;
        }

        if (MaxLength.HasValue && textInstance.Value.Length > MaxLength)
        {
            errors.Add($"Text length can't be greater than {MaxLength}");
        }

        return errors;
    }
#endregion
    public override void UpdateAttribute(AttributeConfiguration updatedAttribute)
    {
        var updated = updatedAttribute as TextAttributeConfiguration;

        if (updated == null)
        {
            throw new ArgumentException("Invalid attribute type");
        }

        base.UpdateAttribute(updatedAttribute);

        if (DefaultValue != updated.DefaultValue
            || MaxLength != updated.MaxLength
            || IsSearchable != updated.IsSearchable
           )
        {
            Apply(new TextAttributeConfigurationUpdated(Id,
                    updated.DefaultValue,
                    updated.MaxLength,
                    updated.IsSearchable
                )
            );
        }
    }

    #region Equality
    public override bool Equals(object obj)
    {
        return Equals(obj as TextAttributeConfiguration);
    }

    private bool Equals(TextAttributeConfiguration other)
    {
        return base.Equals(other)
               && DefaultValue.Equals(other.DefaultValue)
               && Nullable.Equals(MaxLength, other.MaxLength)
               && IsSearchable == other.IsSearchable
               && ValueType == other.ValueType;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DefaultValue, MaxLength, IsSearchable, (int)ValueType);
    }

    #endregion

    #region EventHandlers

    public void On(TextAttributeConfigurationUpdated @event)
    {
        DefaultValue = @event.DefaultValue;
        MaxLength = @event.MaxLength;
        IsSearchable = @event.IsSearchable;
    }

    #endregion
}

using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes;

public class DateRangeAttributeConfiguration : AttributeConfiguration

{
    public DateRangeAttributeType DateRangeAttributeType { get; set; }

    public override EavAttributeType ValueType => EavAttributeType.DateRange;
    #region Init
    public DateRangeAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
    {
    }

    public DateRangeAttributeConfiguration(Guid id,
        string machineName,
        List<LocalizedString> name,
        DateRangeAttributeType dateRangeAttributeType,
        List<LocalizedString>? description = null,
        bool isRequired = false,
        Guid? tenantId = null,
        string? metadata = null
    ) : base(id, machineName, name, EavAttributeType.DateRange, description, isRequired, tenantId, metadata)
    {
        Apply(new DateRangeAttributeConfigurationUpdated(id, dateRangeAttributeType));
    }

    public DateRangeAttributeConfiguration(string machineName, Guid? tenantId) : this(Guid.NewGuid(),
        machineName,
        new List<LocalizedString>
        {
            LocalizedString.English("Date")
        },
        DateRangeAttributeType.SingleDate,
        new List<LocalizedString>
        {
            LocalizedString.English("Date")
        },
        tenantId: tenantId)
    {

    }
    #endregion

    #region Validation
    public override List<string> Validate()
    {
        List<string> errors = base.Validate();
        if (!Enum.IsDefined(typeof(DateRangeAttributeType), DateRangeAttributeType))
        {
            errors.Add("Unknown date attribute type");
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

        if (instance is not DateRangeAttributeInstance)
        {
            errors.Add("Cannot validate attribute. Expected attribute type: DateRange)");
            return errors;
        }

        return errors;
    }
    #endregion

    public override void UpdateAttribute(AttributeConfiguration updatedAttribute)
    {
        var updated = updatedAttribute as DateRangeAttributeConfiguration;

        if (updated == null)
        {
            throw new ArgumentException("Invalid attribute type");
        }

        base.UpdateAttribute(updatedAttribute);

        if (DateRangeAttributeType != updated.DateRangeAttributeType)
        {
            Apply(new DateRangeAttributeConfigurationUpdated(Id, updated.DateRangeAttributeType));
        }
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as DateRangeAttributeConfiguration);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), DateRangeAttributeType);
    }

    private bool Equals(DateRangeAttributeConfiguration other)
    {
        return base.Equals(other)
               && DateRangeAttributeType.Equals(other.DateRangeAttributeType)
               && ValueType == other.ValueType;
    }

    #region EventHandlers

    public void On(DateRangeAttributeConfigurationUpdated @event)
    {
        DateRangeAttributeType = @event.DateRangeAttributeType;
    }

    #endregion
}

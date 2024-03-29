using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes;

public class BooleanAttributeConfiguration : AttributeConfiguration
{
    public string TrueDisplayValue { get; set; }

    public string FalseDisplayValue { get; set; }

    public override EavAttributeType ValueType { get; } = EavAttributeType.Boolean;

    #region Init

    public BooleanAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
    {
    }

    public BooleanAttributeConfiguration(
        Guid id,
        string machineName,
        List<LocalizedString> name,
        string trueDisplayValue,
        string falseDisplayValue,
        List<LocalizedString>? description = null,
        bool isRequired = false,
        Guid? tenantId = null,
        string? metadata = null
    ) : base(id, machineName, name, EavAttributeType.Boolean, description, isRequired, tenantId, metadata)
    {
        Apply(new BooleanAttributeConfigurationUpdated(id, trueDisplayValue, falseDisplayValue));
    }

    public BooleanAttributeConfiguration(string machineName, Guid? tenantId) : this(Guid.NewGuid(),
        machineName,
        new List<LocalizedString>()
        {
            LocalizedString.English("Boolean Attribute")
        },
        "True",
        "False",
        tenantId: tenantId)
    {

    }

    #endregion

    #region Validation

    public override List<string> Validate()
    {
        List<string> errors = base.Validate();
        if (string.IsNullOrEmpty(TrueDisplayValue) || string.IsNullOrEmpty(FalseDisplayValue))
        {
            errors.Add("Values descriptions should be specified");
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

        if (instance is not BooleanAttributeInstance)
        {
            errors.Add("Cannot validate attribute. Expected attribute type: Boolean");
            return errors;
        }

        return errors;
    }

    #endregion
    public override void UpdateAttribute(AttributeConfiguration updatedAttribute)
    {
        var updated = updatedAttribute as BooleanAttributeConfiguration;

        if (updated == null)
        {
            throw new ArgumentException("Invalid attribute type");
        }

        base.UpdateAttribute(updatedAttribute);

        if (TrueDisplayValue != updated.TrueDisplayValue || FalseDisplayValue != updated.FalseDisplayValue)
        {
            Apply(new BooleanAttributeConfigurationUpdated(Id, updated.TrueDisplayValue, updated.FalseDisplayValue)
            );
        }
    }

    #region Equality

    public override bool Equals(object obj)
    {
        return Equals(obj as BooleanAttributeConfiguration);
    }

    private bool Equals(BooleanAttributeConfiguration other)
    {
        return base.Equals(other)
               && TrueDisplayValue == other.TrueDisplayValue
               && FalseDisplayValue == other.FalseDisplayValue
               && ValueType == other.ValueType;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TrueDisplayValue, FalseDisplayValue, (int)ValueType);
    }

    #endregion
    #region EventHandlers

    public void On(BooleanAttributeConfigurationUpdated @event)
    {
        Id = @event.AggregateId;
        TrueDisplayValue = @event.TrueDisplayValue;
        FalseDisplayValue = @event.FalseDisplayValue;
    }

    #endregion
}

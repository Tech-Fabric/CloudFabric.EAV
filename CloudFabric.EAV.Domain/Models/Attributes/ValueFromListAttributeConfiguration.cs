using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes;

public class ValueFromListAttributeConfiguration : AttributeConfiguration
{
    // ReSharper disable once UnusedMember.Global
    // This constructor is required for Event Store to properly deserialize from json
    public ValueFromListAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
    {
    }

    public ValueFromListAttributeConfiguration(Guid id,
        string machineName,
        List<LocalizedString> name,
        List<ValueFromListOptionConfiguration> valuesList,
        List<LocalizedString>? description = null,
        bool isRequired = false,
        Guid? tenantId = null,
        string? metadata = null
    ) : base(id, machineName, name, EavAttributeType.ValueFromList, description, isRequired, tenantId, metadata)
    {
        ValuesList = valuesList;
        Apply(new ValueFromListConfigurationUpdated(id, valuesList));
    }

    public List<ValueFromListOptionConfiguration> ValuesList { get; set; }
    public override EavAttributeType ValueType => EavAttributeType.ValueFromList;

    public override List<string> Validate()
    {
        List<string> errors = base.Validate();

        if (ValuesList.Count == 0)
        {
            errors.Add("Cannot create attribute without options");
        }

        if (ValuesList.GroupBy(x => x.MachineName).Any(x => x.Count() > 1)
            || ValuesList.GroupBy(x => x.Name).Any(x => x.Count() > 1))
        {
            errors.Add("Identical options not allowed");
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

        if (instance is not ValueFromListAttributeInstance valueInstance)
        {
            errors.Add("Cannot validate attribute. Expected attribute type: Value from list");
            return errors;
        }

        if (!ValuesList.Any(x => x.MachineName == valueInstance.Value))
        {
            errors.Add("Cannot validate attribute. Wrong option");
            return errors;
        }

        return errors;
    }

    public override void UpdateAttribute(AttributeConfiguration updatedAttribute)
    {
        var updated = updatedAttribute as ValueFromListAttributeConfiguration;

        if (updated == null)
        {
            throw new ArgumentException("Invalid attribute type");
        }

        base.UpdateAttribute(updatedAttribute);

        if (!ValuesList.Equals(updated.ValuesList)
           )
        {
            Apply(new ValueFromListConfigurationUpdated(Id, updated.ValuesList));
        }
    }

    protected bool Equals(ValueFromListAttributeConfiguration other)
    {
        return base.Equals(other)
               && ValuesList.Equals(other.ValuesList);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((ValueFromListAttributeConfiguration)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), ValuesList);
    }

    #region EventHandlers

    public void On(ValueFromListConfigurationUpdated @event)
    {
        ValuesList = @event.ValueFromListOptions;
    }

    #endregion
}

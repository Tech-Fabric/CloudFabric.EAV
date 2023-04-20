using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Serialization;

using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attribute;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Json.Utilities;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models;

[JsonConverter(typeof(PolymorphicJsonConverter<AttributeConfiguration>))]
public abstract class AttributeConfiguration : AggregateBase
{
    public AttributeConfiguration(IEnumerable<IEvent> events) : base(events)
    {
    }

    public AttributeConfiguration(
        Guid id,
        string machineName,
        List<LocalizedString> name,
        EavAttributeType valueType,
        List<LocalizedString>? description = null,
        bool isRequired = false,
        Guid? tenantId = null,
        string? metadata = null
    )
    {
        Apply(new AttributeConfigurationCreated(id,
                machineName,
                name,
                valueType,
                description,
                isRequired,
                tenantId,
                metadata
            )
        );
    }

    public override string PartitionKey => Id.ToString();

    public string MachineName { get; protected set; }

    public static string DefaultMachineName { get; }

    public ReadOnlyCollection<LocalizedString> Name { get; protected set; }

    public ReadOnlyCollection<LocalizedString> Description { get; protected set; }

    public bool IsRequired { get; protected set; }

    public abstract EavAttributeType ValueType { get; }

    public Guid? TenantId { get; protected set; }

    public bool IsDeleted { get; protected set; }

    public string? Metadata { get; protected set; }

    # region Validation

    public virtual List<string> Validate()
    {
        var errors = new List<string>();
        if (Name.Count == 0)
        {
            errors.Add("Name cannot be empty");
        }

        if (string.IsNullOrEmpty(MachineName) || string.IsNullOrWhiteSpace(MachineName))
        {
            errors.Add("MachineName cannot be empty");
        }

        if (!Enum.IsDefined(typeof(EavAttributeType), ValueType))
        {
            errors.Add("Unknown value type");
        }

        return errors;
    }

    public virtual List<string> ValidateInstance(AttributeInstance? instance)
    {
        if (!IsRequired)
        {
            return new List<string>();
        }
        return instance?.GetValue() == null ? new List<string> { "Attribute is Required" } : new List<string>();
    }

    #endregion

    public void UpdateName(string newName)
    {
        Apply(new AttributeConfigurationNameUpdated(Id,
                newName,
                CultureInfo.GetCultureInfo("en-US").LCID
            )
        );
    }

    public void UpdateName(string newName, int cultureInfoId)
    {
        Apply(new AttributeConfigurationNameUpdated(Id, newName, cultureInfoId));
    }

    public void UpdateDescription(string newDescription)
    {
        Apply(new AttributeConfigurationDescriptionUpdated(Id,
                newDescription,
                CultureInfo.GetCultureInfo("en-US").LCID
            )
        );
    }

    public void UpdateDescription(string newDescription, int cultureInfoId)
    {
        Apply(new AttributeConfigurationDescriptionUpdated(Id, newDescription, cultureInfoId));
    }

    public void UpdateIsRequiredFlag(bool newIsRequiredFlag)
    {
        Apply(new AttributeConfigurationIsRequiredFlagUpdated(Id, newIsRequiredFlag));
    }

    public virtual void UpdateAttribute(AttributeConfiguration updatedAttribute)
    {
        if (!Equals(updatedAttribute))
        {
            Apply(
                new AttributeConfigurationUpdated(
                    Id,
                    updatedAttribute.Name.ToList(),
                    updatedAttribute.Description.ToList(),
                    updatedAttribute.IsRequired,
                    updatedAttribute.TenantId,
                    updatedAttribute.Metadata
                )
            );
        }
    }

    public void Delete()
    {
        Apply(new AttributeConfigurationDeleted(Id));
    }

    #region Equality

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj.GetType() == GetType() && Equals(obj as AttributeConfiguration);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsRequired, Name, Description, MachineName, (int)ValueType, TenantId, Metadata);
    }

    private bool Equals(AttributeConfiguration obj)
    {
        return obj != null
               && Name.SequenceEqual(obj.Name)
               && Description.SequenceEqual(obj.Description)
               && IsRequired.Equals(obj.IsRequired)
               && MachineName.Equals(obj.MachineName)
               && ValueType.Equals(obj.ValueType)
               && TenantId == obj.TenantId
               && Metadata == obj.Metadata;
    }

    #endregion



    #region EventHandlers

    public void On(AttributeConfigurationCreated @event)
    {
        Id = @event.AggregateId;
        MachineName = @event.MachineName;
        Name = new List<LocalizedString>(@event.Name).AsReadOnly();
        Description = @event.Description == null
            ? new List<LocalizedString>().AsReadOnly()
            : new List<LocalizedString>(@event.Description).AsReadOnly();
        IsRequired = @event.IsRequired;
        TenantId = @event.TenantId;
        Metadata = @event.Metadata;
    }

    public void On(AttributeConfigurationNameUpdated @event)
    {
        LocalizedString? name = Name.FirstOrDefault(n => n.CultureInfoId == @event.CultureInfoId);

        var newCollection = new List<LocalizedString>(Name);
        if (name == null)
        {
            newCollection.Add(
                new LocalizedString { CultureInfoId = @event.CultureInfoId, String = @event.NewName }
            );
        }
        else
        {
            name.String = @event.NewName;
        }

        Name = newCollection.AsReadOnly();
    }

    public void On(AttributeConfigurationDescriptionUpdated @event)
    {
        LocalizedString? description = Description.FirstOrDefault(n => n.CultureInfoId == @event.CultureInfoId);

        var newCollection = new List<LocalizedString>(Description);
        if (description == null)
        {
            newCollection.Add(
                new LocalizedString { CultureInfoId = @event.CultureInfoId, String = @event.NewDescription }
            );
        }
        else
        {
            description.String = @event.NewDescription;
        }

        Description = newCollection.AsReadOnly();
    }

    public void On(AttributeConfigurationIsRequiredFlagUpdated @event)
    {
        IsRequired = @event.NewIsRequired;
    }

    public void On(AttributeConfigurationUpdated @event)
    {
        Name = @event.Name.AsReadOnly();
        Description = @event.Description.AsReadOnly();
        IsRequired = @event.IsRequired;
        TenantId = @event.TenantId;
        Metadata = @event.Metadata;
    }

    public void On(AttributeConfigurationDeleted @event)
    {
        IsDeleted = true;
    }

    #endregion
}

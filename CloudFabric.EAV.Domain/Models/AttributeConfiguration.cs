using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Serialization;

using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attribute;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Json.Utilities;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeConfiguration>))]
    public abstract class AttributeConfiguration : AggregateBase
    {
        public override string PartitionKey { get => Id.ToString(); }

        public string MachineName { get; protected set; }

        public ReadOnlyCollection<LocalizedString> Name { get; protected set; }

        public ReadOnlyCollection<LocalizedString> Description { get; protected set; }

        public bool IsRequired { get; protected set; }

        public abstract EavAttributeType ValueType { get; }

        public Guid? TenantId { get; protected set; }

        public bool IsDeleted { get; protected set; }

        // attribute configuration cannot be updated (only created and deleted)
        public bool IsReadOnly { get; protected set; }

        public virtual List<string> Validate(AttributeInstance? instance)
        {
            if (IsRequired && instance == null)
            {
                return new List<string>() { "Attribute is Required" };
            }

            return new List<string>();
        }

        public AttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {
        }

        public AttributeConfiguration(
            Guid id,
            string machineName,
            List<LocalizedString> name,
            EavAttributeType valueType,
            List<LocalizedString> description = null,
            bool isRequired = false,
            Guid? tenantId = null,
            bool isReadOnly = false
        )
        {
            Apply(new AttributeConfigurationCreated(id, machineName, name, valueType, description, isRequired, tenantId, isReadOnly));
        }

        public virtual void InitializeInstance(AttributeInstance instance)
        {
            return;
        }

        public void UpdateName(string newName)
        {
            Apply(new AttributeConfigurationNameUpdated(Id,
                    newName,
                    CultureInfo.GetCultureInfo("EN-us").LCID
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
                    CultureInfo.GetCultureInfo("EN-us").LCID
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
                        updatedAttribute.TenantId
                    )
                );
            }
        }

        public void Delete()
        {
            Apply(new AttributeConfigurationDeleted(Id));
        }

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
            return HashCode.Combine(IsRequired, Name, Description, MachineName, (int)ValueType, IsReadOnly);
        }

        private bool Equals(AttributeConfiguration obj)
        {
            return obj != null
                   && (Name.SequenceEqual(obj.Name)
                       && Description.SequenceEqual(obj.Description)
                       && IsRequired.Equals(obj.IsRequired)
                       && MachineName.Equals(obj.MachineName)
                       && ValueType.Equals(obj.ValueType)
                       && IsReadOnly.Equals(obj.IsReadOnly));
        }

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
            IsReadOnly = @event.IsReadOnly;
        }

        public void On(AttributeConfigurationNameUpdated @event)
        {
            var name = Name.FirstOrDefault(n => n.CultureInfoId == @event.CultureInfoId);

            var newCollection = new List<LocalizedString>(Name);
            if (name == null)
            {
                newCollection.Add(
                    new LocalizedString
                    {
                        CultureInfoId = @event.CultureInfoId,
                        String = @event.NewName
                    }
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
            var description = Description.FirstOrDefault(n => n.CultureInfoId == @event.CultureInfoId);

            var newCollection = new List<LocalizedString>(Description);
            if (description == null)
            {
                newCollection.Add(
                    new LocalizedString
                    {
                        CultureInfoId = @event.CultureInfoId,
                        String = @event.NewDescription
                    }
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
        }

        public void On(AttributeConfigurationDeleted @event)
        {
            IsDeleted = true;
        }

        #endregion
    }
}
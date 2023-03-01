using System.Collections.ObjectModel;
using System.Globalization;

using CloudFabric.EAV.Domain.Events.Configuration.Entity;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models
{
    public class EntityConfiguration : AggregateBase
    {
        public ReadOnlyCollection<LocalizedString> Name { get; protected set; }

        public string MachineName { get; protected set; }

        public ReadOnlyCollection<EntityConfigurationAttributeReference> Attributes { get; protected set; }

        public override string PartitionKey => Id.ToString();

        public Guid? TenantId { get; protected set; }

        public EntityConfiguration(List<IEvent> events) : base(events)
        {

        }

        public EntityConfiguration(
            Guid id,
            List<LocalizedString> name,
            string machineName,
            List<EntityConfigurationAttributeReference> attributes,
            Guid? tenantId
        )
        {
            Apply(new EntityConfigurationCreated(
                id,
                name,
                machineName,
                attributes,
                tenantId
            ));
        }

        public List<string> Validate()
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
            return errors;
        }
        public void UpdateName(string newName)
        {
            Apply(new EntityConfigurationNameUpdated(Id, newName, CultureInfo.GetCultureInfo("EN-us").LCID));
        }

        public void UpdateName(string newName, int cultureInfoId)
        {
            Apply(new EntityConfigurationNameUpdated(Id, newName, cultureInfoId));
        }

        public void AddAttribute(Guid attributeConfigurationId)
        {
            Apply(new EntityConfigurationAttributeAdded(
                Id,
                new EntityConfigurationAttributeReference() { AttributeConfigurationId = attributeConfigurationId }
            ));
        }

        public void RemoveAttribute(Guid attributeConfigurationId)
        {
            Apply(new EntityConfigurationAttributeRemoved(Id, attributeConfigurationId));
        }

        public void UpdateAttrributeExternalValues(Guid attributeId, List<object> values)
        {
            var attributeReference = Attributes.FirstOrDefault(a => a.AttributeConfigurationId == attributeId);

            if (attributeReference == null)
            {
                throw new NotFoundException("Attribute not found");
            }

            Apply(new EntityConfigurationAttributeExternalValuesUpdated(Id, attributeId, values));
        }

        #region EventHandlers
        public void On(EntityConfigurationCreated @event)
        {
            Id = @event.AggregateId;
            Name = new List<LocalizedString>(@event.Name).AsReadOnly();
            MachineName = @event.MachineName;
            Attributes = new List<EntityConfigurationAttributeReference>(@event.Attributes).AsReadOnly();
            TenantId = @event.TenantId;
        }

        public void On(EntityConfigurationNameUpdated @event)
        {
            var newCollection = new List<LocalizedString>(Name);
            var nameIndex = newCollection.FindIndex(s => s.CultureInfoId == @event.CultureInfoId);
            var name = newCollection.FirstOrDefault(n => n.CultureInfoId == @event.CultureInfoId);

            if (nameIndex == -1)
            {
                newCollection.Add(new LocalizedString
                {
                    CultureInfoId = @event.CultureInfoId,
                    String = @event.NewName
                });
            }
            else
            {
                newCollection[nameIndex] = new LocalizedString
                {
                    CultureInfoId = @event.CultureInfoId,
                    String = @event.NewName
                };
            }

            Name = newCollection.AsReadOnly();
        }

        public void On(EntityConfigurationAttributeAdded @event)
        {
            var newCollection = new List<EntityConfigurationAttributeReference>(Attributes);
            newCollection.Add(@event.AttributeReference);
            Attributes = newCollection.AsReadOnly();
        }

        public void On(EntityConfigurationAttributeRemoved @event)
        {
            Attributes = Attributes
                .Where(a => a.AttributeConfigurationId != @event.AttributeConfigurationId)
                .ToList()
                .AsReadOnly();
        }
        public void On(EntityConfigurationAttributeExternalValuesUpdated @event)
        {
            var attributeReference = Attributes.First(a => a.AttributeConfigurationId == @event.AttributeConfigurationId);

            attributeReference.AttributeConfigurationExternalValues = @event.ExternalValues;
        }
        #endregion
    }
}

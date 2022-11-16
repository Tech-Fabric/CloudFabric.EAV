using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
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

        public ReadOnlyCollection<AttributeConfiguration> Attributes { get; protected set; }

        public override string PartitionKey => Id.ToString();
        
        public Guid? TenantId { get; protected set; }

        public ReadOnlyDictionary<string, object> Metadata { get; protected set; }

        public EntityConfiguration(List<IEvent> events) : base(events)
        {

        }

        public EntityConfiguration(Guid id,
            List<LocalizedString> name,
            string machineName,
            List<AttributeConfiguration> attributes,
            Guid? tenantId = null,
            Dictionary<string, object> metadata = null
        )
        {
            Apply(new EntityConfigurationCreated(id, name, machineName, attributes, tenantId, metadata));
        }

        public void UpdateName(string newName)
        {
            Apply(new EntityConfigurationNameUpdated(Id, newName, CultureInfo.GetCultureInfo("EN-us").LCID));
        }

        public void UpdateName(string newName, int cultureInfoId)
        {
            Apply(new EntityConfigurationNameUpdated(Id, newName, cultureInfoId));
        }

        public void AddAttribute(AttributeConfiguration attributeConfiguration)
        {
            Apply(new EntityConfigurationAttributeAdded(Id, attributeConfiguration));
        }

        public void UpdateAttribute(AttributeConfiguration attributeConfiguration)
        {
            Apply(new EntityConfigurationAttributeUpdated(Id, attributeConfiguration));
        }

        public void RemoveAttribute(AttributeConfiguration attributeConfiguration)
        {
            Apply(new EntityConfigurationAttributeRemoved(Id, attributeConfiguration.MachineName));
        }

        public void UpdateMetadata(Dictionary<string, object> newMetadata)
        {
            Apply(new EntityConfigurationMetadataUpdated(Id, newMetadata));
        }

        #region EventHandlers
        
        public void On(EntityConfigurationCreated @event)
        {
            Id = @event.Id;
            Name = new List<LocalizedString>(@event.Name).AsReadOnly();
            MachineName = @event.MachineName;
            Attributes = new List<AttributeConfiguration>(@event.Attributes).AsReadOnly();
            TenantId = @event.TenantId;
            Metadata = new ReadOnlyDictionary<string, object>(@event.Metadata ?? new());
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
            var newCollection = new List<AttributeConfiguration>(Attributes);
            newCollection.Add(@event.Attribute);
            Attributes = newCollection.AsReadOnly();
        }

        public void On(EntityConfigurationAttributeUpdated @event)
        {
            var newCollection = new List<AttributeConfiguration>(Attributes);
            var attrIndex = newCollection.FindIndex(a => a.MachineName == @event.Attribute.MachineName);
            if (attrIndex != -1)
            {
                newCollection[attrIndex] = @event.Attribute;
            }
            Attributes = newCollection.AsReadOnly();
        }

        public void On(EntityConfigurationAttributeRemoved @event)
        {
            Attributes = Attributes
                .Where(a => a.MachineName != @event.AttributeMachineName)
                .ToList()
                .AsReadOnly();
        }

        public void On(EntityConfigurationMetadataUpdated @event)
        {
            Metadata = new ReadOnlyDictionary<string, object>(@event.Metadata ?? new());
        }
        
        #endregion
    }
}
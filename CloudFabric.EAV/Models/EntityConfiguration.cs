using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.PortableExecutable;
using CloudFabric.EAV.Domain.Events.Configuration.Entity;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models
{
    public class EntityConfiguration : AggregateBase
    {
        public static readonly string ENTITY_CONFIGURATION_PARTITION_KEY = "EntityConfiguration";
        public List<LocalizedString> Name { get; protected set; }
        
        public string MachineName { get; protected set; }


        public List<AttributeConfiguration> Attributes { get; protected set; }

        public override string PartitionKey => ENTITY_CONFIGURATION_PARTITION_KEY;

        public EntityConfiguration(List<IEvent> events) : base(events)
        {
            
        }

        public EntityConfiguration(Guid id, List<LocalizedString> name, string machineName, List<AttributeConfiguration> attributes)
        {
            Apply(new EntityConfigurationCreated(id, name, machineName, attributes));
        }

        public void ChangeName(string newName)
        {
            Apply(new EntityConfigurationNameChanged(Guid.Parse(Id), newName, CultureInfo.GetCultureInfo("EN-us").LCID));
        }
        
        public void ChangeName(string newName, int cultureInfoId)
        {
            Apply(new EntityConfigurationNameChanged(Guid.Parse(Id), newName, cultureInfoId));
        }

        public void AddAttribute(AttributeConfiguration attributeConfiguration)
        {
            Apply(new EntityConfigurationAttributeAdded(Guid.Parse(Id), attributeConfiguration));
        }

        public void UpdateAttribute(AttributeConfiguration attributeConfiguration)
        {
            Apply(new EntityConfigurationAttributeUpdated(Guid.Parse(Id), attributeConfiguration));
        }

        public void RemoveAttribute(string attributeMachineName)
        {
            Apply(new EntityConfigurationAttributeRemoved(Guid.Parse(Id), attributeMachineName));
        }
        
        #region EventHandlers

        public void On(EntityConfigurationCreated @event)
        {
            Id = @event.Id.ToString();
            Name = @event.Name;
            MachineName = @event.MachineName;
            Attributes = @event.Attributes;
        }
        
        public void On(EntityConfigurationNameChanged @event)
        {
            var name = Name.FirstOrDefault(n => n.CultureInfoId == @event.CultureInfoId);

            if (name == null)
            {
                Name.Add(new LocalizedString()
                {
                    CultureInfoId = @event.CultureInfoId,
                    String = @event.NewName
                });
            }
            else
            {
                name.String = @event.NewName;
            }
        }

        public void On(EntityConfigurationAttributeAdded @event)
        {
            Attributes ??= new();
            Attributes.Add(@event.Attribute);
        }

        public void On(EntityConfigurationAttributeUpdated @event)
        {
            var attributeToRemove = Attributes?.FirstOrDefault(x => x.MachineName == @event.Attribute.MachineName);

            if (attributeToRemove != null)
            {
                Attributes.Remove(attributeToRemove);
                Attributes.Add(@event.Attribute);
            }
        }

        public void On(EntityConfigurationAttributeRemoved @event)
        {
            var attributeToRemove = Attributes?.FirstOrDefault(x => x.MachineName == @event.AttributeMachineName);

            if (attributeToRemove != null)
            {
                Attributes.Remove(attributeToRemove);
            }
        }

        #endregion
    }
}
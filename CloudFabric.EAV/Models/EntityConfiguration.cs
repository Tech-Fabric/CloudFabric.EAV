using System;
using System.Collections.Generic;
using System.Linq;
using CloudFabric.EAV.Data.Events.Configuration.Entity;
using CloudFabric.EAV.Data.Models.Base;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Models
{
    public class EntityConfiguration : AggregateBase
    {

        public List<LocalizedString> Name { get; protected set; }

        public string MachineName { get; protected set; }


        public List<AttributeConfiguration> Attributes { get; protected set; }

        public List<EntityInstance> EntityInstances { get; protected set; }

        public EntityConfiguration(List<IEvent> events) : base(events)
        {

        }

        public EntityConfiguration(List<LocalizedString> name, string machineName, List<AttributeConfiguration> attributes)
        {
            Apply(new EntityConfigurationCreated(name, machineName, attributes));
        }

        public void AddAttributeConfiguration(AttributeConfiguration configuration)
        {
            Apply(new AddAttributeConfiguration(configuration));
        }

        public void RemoveAttributeConfiguration(Guid id)
        {
            Apply(new RemoveAttributeConfiguration(id));
        }

        public void UpdateAttributeConfiguration(Guid id, AttributeConfiguration configuration)
        {
            //TODO: Add
        }
        
        public void ChangeName(List<LocalizedString> newName)
        {
            Apply(new ChangeEntityConfigName(newName));
        }

        #region Events

        public void On(EntityConfigurationCreated @event)
        {
            Name = @event.Name;
            MachineName = @event.MachineName;
            Attributes = @event.Attributes;
        }
        public void On(AddAttributeConfiguration @event)
        {
            //TODO: Add position
            Attributes.Add(@event.Configuration);
        }

        public void On(RemoveAttributeConfiguration @event)
        {
            Attributes = Attributes.Where(a => a.Id != @event.Id).ToList();
        }
        
        public void On(ChangeEntityConfigName @event)
        {
            Name = @event.Name;
        }

        #endregion
    }
}
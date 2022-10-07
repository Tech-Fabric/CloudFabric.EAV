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
        public List<LocalizedString> Name { get; protected set; }
        
        public string MachineName { get; protected set; }


        public List<AttributeConfiguration> Attributes { get; protected set; }

        public override string PartitionKey => "EntityConfiguration";

        public EntityConfiguration(List<IEvent> events) : base(events)
        {
            
        }

        public EntityConfiguration(Guid id, List<LocalizedString> name, string machineName, List<AttributeConfiguration> attributes)
        {
            Apply(new EntityConfigurationCreated(id, name, machineName, attributes));
        }

        public void ChangeName(string newName)
        {
            Apply(new EntityConfigurationNameChanged(newName, CultureInfo.GetCultureInfo("EN-us").LCID));
        }
        
        public void ChangeName(string newName, int cultureInfoId)
        {
            Apply(new EntityConfigurationNameChanged(newName, cultureInfoId));
        }

        public void AddAttribute(AttributeConfiguration attributeConfiguration)
        {
            Apply(new EntityConfigurationAttributeAdded(attributeConfiguration));
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
            var name = Name.FirstOrDefault(n => n.CultureInfoId == @event.cultureInfoId);

            if (name == null)
            {
                Name.Add(new LocalizedString()
                {
                    CultureInfoId = @event.cultureInfoId,
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
            Attributes.Add(@event.Attribute);
        }
        #endregion
    }
}
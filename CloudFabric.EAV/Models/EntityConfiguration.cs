using System.Collections.Generic;
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
    }
}
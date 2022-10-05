using System.Collections.Generic;
using CloudFabric.EAV.Json.Utilities;

using System.Text.Json.Serialization;
using CloudFabric.EAV.Data.Events.Instance.Attribute;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Models
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeInstance>))]
    public class AttributeInstance: AggregateBase
    {
        public string ConfigurationAttributeMachineName { get; protected set; }

        public AttributeInstance(IEnumerable<IEvent> events) : base(events)
        {
        }

        public AttributeInstance(string configurationAttributeMachineName)
        {
            Apply(new AttributeInstanceCreated(configurationAttributeMachineName));
        }
    }
}
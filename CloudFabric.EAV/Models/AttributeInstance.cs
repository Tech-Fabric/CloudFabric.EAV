using System.Collections.Generic;
using CloudFabric.EAV.Json.Utilities;

using System.Text.Json.Serialization;
using CloudFabric.EAV.Domain.Events.Instance.Attribute;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeInstance>))]
    public class AttributeInstance
    {
        public string ConfigurationAttributeMachineName { get; protected set; }
    }
}
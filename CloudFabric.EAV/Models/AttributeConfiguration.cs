using System;
using CloudFabric.EAV.Data.Enums;
using CloudFabric.EAV.Data.Models.Base;
using CloudFabric.EAV.Json.Utilities;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CloudFabric.EAV.Data.Events.Configuration.Attributes;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Models
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeConfiguration>))]
    public abstract class AttributeConfiguration: AggregateBase
    {
        public List<LocalizedString> Name { get; protected set; }

        public List<LocalizedString> Description { get; protected set; }

        public string MachineName { get; protected set; }

        public List<AttributeValidationRule> ValidationRules { get; protected set; }

        public abstract EavAttributeType ValueType { get; }

        public AttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {
            
        }

        public AttributeConfiguration(
            List<LocalizedString> name, List<LocalizedString> description, string machineName, List<AttributeValidationRule> validationRules
        )
        {
            Apply(new AttributeCreated(name, description, machineName, validationRules));
        }
    }
}
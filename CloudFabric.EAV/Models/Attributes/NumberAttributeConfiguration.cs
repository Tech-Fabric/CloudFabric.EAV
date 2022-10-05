using System.Collections.Generic;
using CloudFabric.EAV.Data.Enums;
using CloudFabric.EAV.Data.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Models.Attributes
{
    public class NumberAttributeConfiguration : AttributeConfiguration
    {
        public float DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Number;

        public NumberAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {
        }

        public NumberAttributeConfiguration(List<LocalizedString> name, List<LocalizedString> description, string machineName, List<AttributeValidationRule> validationRules) : base(name, description, machineName, validationRules)
        {
        }
    }
}
using System.Collections.Generic;
using CloudFabric.EAV.Data.Enums;
using CloudFabric.EAV.Data.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Models.Attributes
{
    public class ArrayAttributeConfiguration : AttributeConfiguration
    {
        public override EavAttributeType ValueType { get; } = EavAttributeType.Array;

        public EavAttributeType ItemsType { get; set; }

        public AttributeConfiguration ItemsAttributeConfiguration { get; set; }

        public ArrayAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {
        }

        public ArrayAttributeConfiguration(List<LocalizedString> name, List<LocalizedString> description, string machineName, List<AttributeValidationRule> validationRules) : base(name, description, machineName, validationRules)
        {
        }
    }
}
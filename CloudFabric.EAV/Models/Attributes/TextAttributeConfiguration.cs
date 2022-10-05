using System.Collections.Generic;
using CloudFabric.EAV.Data.Enums;
using CloudFabric.EAV.Data.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Models.Attributes
{
    public class TextAttributeConfiguration : AttributeConfiguration
    {
        public string DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Text;

        public TextAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {
        }

        public TextAttributeConfiguration(List<LocalizedString> name, List<LocalizedString> description, string machineName, List<AttributeValidationRule> validationRules) : base(name, description, machineName, validationRules)
        {
        }
    }
}
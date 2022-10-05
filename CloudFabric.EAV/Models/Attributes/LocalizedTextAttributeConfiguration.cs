using System.Collections.Generic;
using CloudFabric.EAV.Data.Enums;
using CloudFabric.EAV.Data.Models.Base;
using System.ComponentModel.DataAnnotations.Schema;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Models.Attributes
{
    public class LocalizedTextAttributeConfiguration : AttributeConfiguration
    {

        public LocalizedString DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Text;

        public LocalizedTextAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {
        }

        public LocalizedTextAttributeConfiguration(List<LocalizedString> name, List<LocalizedString> description, string machineName, List<AttributeValidationRule> validationRules) : base(name, description, machineName, validationRules)
        {
        }
    }
}

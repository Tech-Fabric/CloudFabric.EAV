using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class LocalizedTextAttributeConfiguration : AttributeConfiguration
    {

        public LocalizedString DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Text;
        public override (bool, List<string>) Validate(AttributeInstance instance)
        {
            return (true, new List<string>());
        }
    }
}

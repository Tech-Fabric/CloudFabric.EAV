using System.Collections.Generic;
using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class TextAttributeConfiguration : AttributeConfiguration
    {
        public string DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Text;
    }
}
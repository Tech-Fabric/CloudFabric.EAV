using System.Collections.Generic;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class TextAttributeInstance : AttributeInstance
    {
        public string Value { get; set; }
    }
}
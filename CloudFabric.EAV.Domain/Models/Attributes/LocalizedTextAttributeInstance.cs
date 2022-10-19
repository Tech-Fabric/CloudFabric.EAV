using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class LocalizedTextAttributeInstance : AttributeInstance
    {

        public List<LocalizedString> Value { get; set; }
    }
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CloudFabric.EAV.Data.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Models.Attributes
{
    public class LocalizedTextAttributeInstance : AttributeInstance
    {

        public List<LocalizedString> Value { get; set; }

        public LocalizedTextAttributeInstance(IEnumerable<IEvent> events) : base(events)
        {
        }

        public LocalizedTextAttributeInstance(string configurationAttributeMachineName) : base(configurationAttributeMachineName)
        {
        }
    }
}

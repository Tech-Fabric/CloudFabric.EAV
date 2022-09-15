using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudFabric.EAV.Attributes
{
    public class LocalizedTextAttributeInstance : AttributeInstance
    {
        
        public List<LocalizedString> Value { get; set; }
    }
}

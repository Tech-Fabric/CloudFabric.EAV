using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CloudFabric.EAV.Data.Models.Base;

namespace CloudFabric.EAV.Data.Models.Attributes
{
    public class LocalizedTextAttributeInstance : AttributeInstance
    {

        public List<LocalizedString> Value { get; set; }
    }
}

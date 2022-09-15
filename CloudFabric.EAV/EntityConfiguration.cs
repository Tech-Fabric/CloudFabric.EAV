using CloudFabric.EAV.Data.Models.Base;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudFabric.EAV
{
    public class EntityConfiguration : Model
    {
        
        public List<LocalizedString> Name { get; set; }
        
        public string MachineName { get; set; }

        
        public List<AttributeConfiguration> Attributes { get; set; }

        public List<EntityInstance> EntityInstances { get; set; }
    }
}
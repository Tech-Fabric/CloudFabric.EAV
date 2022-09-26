using System.Collections.Generic;
using CloudFabric.EAV.Data.Models.Base;

namespace CloudFabric.EAV.Data.Models
{
    public class EntityConfiguration : Model
    {

        public List<LocalizedString> Name { get; set; }

        public string MachineName { get; set; }


        public List<AttributeConfiguration> Attributes { get; set; }

        public List<EntityInstance> EntityInstances { get; set; }
    }
}
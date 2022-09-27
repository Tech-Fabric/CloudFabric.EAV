using System;
using System.Collections.Generic;
using CloudFabric.EAV.Data.Models.Base;

namespace CloudFabric.EAV.Data.Models
{
    public class EntityInstance : Model
    {
        public Guid EntityConfigurationId { get; set; }

        public EntityConfiguration EntityConfiguration { get; set; }

        public List<AttributeInstance> Attributes { get; set; }
    }
}
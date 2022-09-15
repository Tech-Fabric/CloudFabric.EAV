using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using CloudFabric.EAV.Data.Models.Base;
using CloudFabric.EAV.Json.Utilities;

namespace CloudFabric.EAV
{
    public class EntityInstance : Model
    {
        public Guid EntityConfigurationId { get; set; }

        public EntityConfiguration EntityConfiguration { get; set; }

        public List<AttributeInstance> Attributes { get; set; }
    }
}
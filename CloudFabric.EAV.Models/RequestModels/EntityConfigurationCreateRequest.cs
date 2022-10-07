using System.Collections.Generic;
using CloudFabric.EAV.Models.RequestModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels
{
    public class EntityConfigurationCreateRequest
    {
        public List<LocalizedStringCreateRequest> Name { get; set; }
        
        public string MachineName { get; set; }

        //[JsonConverter(typeof(PolymorphicJsonConverter<List<AttributeConfigurationCreateUpdateRequest>>))]
        public List<AttributeConfigurationCreateUpdateRequest> Attributes { get; set; }
    }
}
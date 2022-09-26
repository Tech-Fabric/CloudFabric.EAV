using CloudFabric.EAV.Service.Models.RequestModels.Attributes;

using System.Collections.Generic;

namespace CloudFabric.EAV.Service.Models.RequestModels.EAV
{
    public class EntityConfigurationCreateRequest
    {
        public List<LocalizedStringCreateRequest> Name { get; set; }
        
        public string MachineName { get; set; }

        //[JsonConverter(typeof(PolymorphicJsonConverter<List<AttributeConfigurationCreateUpdateRequest>>))]
        public List<AttributeConfigurationCreateUpdateRequest> Attributes { get; set; }
    }
}
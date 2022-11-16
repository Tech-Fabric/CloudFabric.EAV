using System.Collections.Generic;
using CloudFabric.EAV.Models.RequestModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels
{
    public class EntityConfigurationCreateRequest
    {
        public List<LocalizedStringCreateRequest> Name { get; set; }
        
        public string MachineName { get; set; }

        public List<AttributeConfigurationCreateUpdateRequest> Attributes { get; set; }
        
        public Guid? TenantId { get; set; }

        public Dictionary<string, object> Metadata { get; set; }
    }
}
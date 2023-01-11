using CloudFabric.EAV.Models.RequestModels.Attributes;

namespace CloudFabric.EAV.Models.LocalEventSourcingPackages.RequestModels
{
    public class CategoryInstanceCreateRequest
    {
        public Guid EntityConfigurationId { get; set; }
        
        public List<AttributeInstanceCreateUpdateRequest> Attributes { get; set; }
        
        public Guid? TenantId { get; set; }
        
        public string CategoryPath { get; set; }
        
        public Guid ChildEntityConfigurationId { get; set; }

    }
}
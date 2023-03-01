namespace CloudFabric.EAV.Models.RequestModels;

public class EntityInstanceCreateRequest
{
    public Guid EntityConfigurationId { get; set; }

    public List<AttributeInstanceCreateUpdateRequest> Attributes { get; set; }

    public Guid? TenantId { get; set; }
}

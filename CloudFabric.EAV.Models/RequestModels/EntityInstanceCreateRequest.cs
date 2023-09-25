namespace CloudFabric.EAV.Models.RequestModels;

public class EntityInstanceCreateRequest
{
    public Guid EntityConfigurationId { get; set; }

    public List<AttributeInstanceCreateUpdateRequest> Attributes { get; set; }

    public Guid? TenantId { get; set; }
    public string? MachineName { get; set; }

    public List<CategoryPathCreateUpdateRequest> CategoryPaths { get; set; }
}

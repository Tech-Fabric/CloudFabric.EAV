namespace CloudFabric.EAV.Models.RequestModels;

public class CategoryInstanceCreateRequest
{
    public Guid CategoryConfigurationId { get; set; }

    public Guid CategoryTreeId { get; set; }

    public string MachineName { get; set; }

    public List<AttributeInstanceCreateUpdateRequest> Attributes { get; set; }

    public Guid? ParentId { get; set; }

    public Guid? TenantId { get; set; }
}

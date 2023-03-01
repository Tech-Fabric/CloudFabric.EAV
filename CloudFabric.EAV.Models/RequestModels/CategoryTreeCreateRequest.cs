namespace CloudFabric.EAV.Models.RequestModels;

public class CategoryTreeCreateRequest
{
    public string MachineName { get; set; }
    public Guid EntityConfigurationId { get; set; }
    public Guid? TenantId { get; set; }
}

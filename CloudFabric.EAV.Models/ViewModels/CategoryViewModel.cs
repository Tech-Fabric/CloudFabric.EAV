using CloudFabric.EAV.Models.ViewModels.Attributes;

public class CategoryViewModel
{
    public Guid Id { get; set; }
        
    public Guid EntityConfigurationId { get; set; }
        
    public List<AttributeInstanceViewModel> Attributes { get; set; }
        
    public Guid? TenantId { get; set; }

    public string PartitionKey { get; set; }
        
    public Guid CategoryTreeId { get; protected set; }
    public string CategoryPath { get; set; }
}
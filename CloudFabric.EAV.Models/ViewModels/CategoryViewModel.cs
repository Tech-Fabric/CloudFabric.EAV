using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.ViewModels;

public class CategoryViewModel
{
    public Guid Id { get; set; }

    public Guid EntityConfigurationId { get; set; }

    public List<AttributeInstanceViewModel> Attributes { get; set; }

    public Guid? TenantId { get; set; }

    public string PartitionKey { get; set; }

    public List<CategoryPath> CategoryPaths { get; set; }
}

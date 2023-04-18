namespace CloudFabric.EAV.Models.ViewModels;

public class CategoryParentChildrenViewModel
{
    public Guid? ParentId { get; set; }

    public List<CategoryViewModel?> Children { get; set; }
}

namespace CloudFabric.EAV.Models.ViewModels;

public class CategoryPathViewModel
{
    public Guid TreeId { get; set; }
    public string Path { get; set; }
    public Guid? ParentId { get; set; }
    public string ParentMachineName { get; set; }
}

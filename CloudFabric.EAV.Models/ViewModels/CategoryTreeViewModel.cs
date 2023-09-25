namespace CloudFabric.EAV.Models.ViewModels;

public class CategoryTreeViewModel
{
    public Guid Id { get; set; }
    public string MachineName { get; protected set; }
    public Guid EntityConfigurationId { get; protected set; }
}

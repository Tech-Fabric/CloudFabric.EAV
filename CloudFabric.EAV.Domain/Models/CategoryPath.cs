namespace CloudFabric.EAV.Domain.Models;

public record CategoryPath
{
    public Guid TreeId { get; set; }
    public string? Path { get; set; }
    public Guid? ParentId { get; set; }
    public string? ParentMachineName { get; set; }
}

namespace CloudFabric.EAV.Models.RequestModels;

public record CategoryPathCreateUpdateRequest
{
    public Guid TreeId { get; set; }

    public string? Path { get; set; }

    public Guid? ParentId { get; set; }
}

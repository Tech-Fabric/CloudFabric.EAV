namespace CloudFabric.EAV.Models.RequestModels;

public class LocalizedStringCreateRequest
{
    public int CultureInfoId { get; set; }
    public string String { get; set; }
}
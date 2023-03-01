namespace CloudFabric.EAV.Models.RequestModels.Attributes;

public class ArrayAttributeInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
{
    public List<AttributeInstanceCreateUpdateRequest> Items { get; set; }
}

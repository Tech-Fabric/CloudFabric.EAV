namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class ValueFromListInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
    {
        public List<string> UnavailableOptionsMachineNames { get; set; }
    }
}
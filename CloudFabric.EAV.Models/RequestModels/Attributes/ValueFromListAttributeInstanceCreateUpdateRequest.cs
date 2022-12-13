namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class ValueFromListAttributeInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
    {
        public List<string> UnavailableOptionsMachineNames { get; set; }
    }
}
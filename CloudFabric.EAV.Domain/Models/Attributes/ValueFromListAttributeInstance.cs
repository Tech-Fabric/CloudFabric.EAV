namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class ValueFromListAttributeInstance : AttributeInstance
    {
        public List<string> UnavailableOptionsMachineNames { get; set; }
        public List<string> PreselectedOptionsMachineNames { get; set; }
    }
}
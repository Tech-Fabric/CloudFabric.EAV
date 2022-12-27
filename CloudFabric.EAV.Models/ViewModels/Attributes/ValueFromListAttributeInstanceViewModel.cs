namespace CloudFabric.EAV.Models.ViewModels.Attributes
{
    public class ValueFromListAttributeInstanceViewModel: AttributeInstanceViewModel
    {
        public List<string> UnavailableOptionsMachineNames { get; set; }
        public List<string> PreselectedOptionsMachineNames { get; set; }
    }
}
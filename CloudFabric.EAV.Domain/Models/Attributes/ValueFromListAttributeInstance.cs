namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class ValueFromListAttributeInstance : AttributeInstance
    {
        public List<string> UnavailableOptionsMachineNames { get; set; }
        public List<string> PreselectedOptionsMachineNames { get; set; }

        public override object? GetValue()
        {
            return this;
        }
    }
}
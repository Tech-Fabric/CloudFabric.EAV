namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class ValueFromListInstance : AttributeInstance
    {
        public List<AttributeInstance> Items { get; set; }
    }
}
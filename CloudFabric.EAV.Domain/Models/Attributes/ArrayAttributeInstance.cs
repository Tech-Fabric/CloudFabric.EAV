namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class ArrayAttributeInstance : AttributeInstance
    {
        public List<AttributeInstance> Items { get; set; }
        public override object? GetValue()
        {
            return Items.Select(i => i.GetValue()).ToList();
        }
    }
}

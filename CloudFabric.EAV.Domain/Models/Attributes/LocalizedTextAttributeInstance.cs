using CloudFabric.EAV.Domain.Models.Base;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class LocalizedTextAttributeInstance : AttributeInstance
    {
        public List<LocalizedString> Value { get; set; }

        public override object? GetValue()
        {
            return Value;
        }
    }
}

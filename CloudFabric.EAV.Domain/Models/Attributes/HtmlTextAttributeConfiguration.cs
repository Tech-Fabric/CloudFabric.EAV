using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Base;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class HtmlTextAttributeConfiguration : AttributeConfiguration
    {
        public string DefaultValue { get; set; }

        public List<string> AllowedTags { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.HtmlText;
        
        public HtmlTextAttributeConfiguration(
            Guid id, 
            string machineName, 
            List<LocalizedString> name,
            List<LocalizedString> description = null, 
            bool isRequired = false,
            Guid? tenantId = null,
            string? metadata = null
        ) : base(id, machineName, name, EavAttributeType.HtmlText, description, isRequired, tenantId, metadata)
        {
        }
    }
}
using CloudFabric.EAV.Data.Enums;
using System.Collections.Generic;

namespace CloudFabric.EAV.Data.Models.Attributes
{
    public class HtmlTextAttributeConfiguration : AttributeConfiguration
    {
        public string DefaultValue { get; set; }

        public List<string> AllowedTags { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.HtmlText;
    }
}
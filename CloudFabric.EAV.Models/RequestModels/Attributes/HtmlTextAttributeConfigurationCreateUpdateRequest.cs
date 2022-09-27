using System.Collections.Generic;
using CloudFabric.EAV.Data.Enums;

namespace CloudFabric.EAV.Service.Models.RequestModels.Attributes
{
    public class HtmlTextAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public string DefaultValue { get; set; }

        public List<string> AllowedTags { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.HtmlText;
    }
}
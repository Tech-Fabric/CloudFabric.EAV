using System.Collections.Generic;
using Nastolkino.Data.Enums;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class HtmlTextAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public string DefaultValue { get; set; }

        public List<string> AllowedTags { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.HtmlText;
    }
}
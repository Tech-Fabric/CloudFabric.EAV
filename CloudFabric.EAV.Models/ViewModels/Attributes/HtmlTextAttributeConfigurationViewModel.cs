using System.Collections.Generic;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class HtmlTextAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public string DefaultValue { get; set; }

        public List<string> AllowedTags { get; set; }
    }
}
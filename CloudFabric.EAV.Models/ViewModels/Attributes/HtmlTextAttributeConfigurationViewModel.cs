using CloudFabric.EAV.Service.Models.ViewModels.Attributes;

using System.Collections.Generic;

namespace CloudFabric.EAV.Service.Models.RequestModels.Attributes
{
    public class HtmlTextAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public string DefaultValue { get; set; }

        public List<string> AllowedTags { get; set; }
    }
}
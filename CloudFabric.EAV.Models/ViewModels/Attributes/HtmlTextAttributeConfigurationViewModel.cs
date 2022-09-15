using Nastolkino.Service.Models.ViewModels.EAV.Attributes;

using System.Collections.Generic;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class HtmlTextAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public string DefaultValue { get; set; }

        public List<string> AllowedTags { get; set; }
    }
}
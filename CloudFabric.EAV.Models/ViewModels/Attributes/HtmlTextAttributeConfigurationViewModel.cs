namespace CloudFabric.EAV.Models.ViewModels.Attributes
{
    public class HtmlTextAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public string DefaultValue { get; set; }

        public List<string> AllowedTags { get; set; }
    }
}

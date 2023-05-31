namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class TextAttributeConfigurationViewModel : AttributeConfigurationViewModel
{
    public string? DefaultValue { get; set; }

    public bool IsSearchable { get; set; }

    public int? MaxLength { get; set; }
}

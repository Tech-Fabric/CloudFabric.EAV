namespace CloudFabric.EAV.Models.ViewModels.Attributes;

public class EntityReferenceAttributeConfigurationViewModel : AttributeConfigurationViewModel
{
    public Guid ReferenceEntityConfiguration { get; set; }

    public Guid? DefaultValue { get; set; }
}

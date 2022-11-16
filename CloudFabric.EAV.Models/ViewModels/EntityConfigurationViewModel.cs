namespace CloudFabric.EAV.Models.ViewModels;

public class EntityConfigurationViewModel
{
    public Guid Id { get; set; }

    public List<LocalizedStringViewModel> Name { get; set; }

    public string PartitionKey { get; set; }

    public string MachineName { get; set; }

    public List<EntityConfigurationAttributeReferenceViewModel> Attributes { get; set; }
}
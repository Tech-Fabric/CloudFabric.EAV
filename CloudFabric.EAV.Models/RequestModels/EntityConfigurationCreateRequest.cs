namespace CloudFabric.EAV.Models.RequestModels;

public class EntityConfigurationCreateRequest
{
    public List<LocalizedStringCreateRequest> Name { get; set; }

    public string MachineName { get; set; }

    public List<EntityAttributeConfigurationCreateUpdateRequest> Attributes { get; set; }

    public Guid? TenantId { get; set; }
}

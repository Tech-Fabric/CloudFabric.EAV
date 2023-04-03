namespace CloudFabric.EAV.Models.RequestModels;

public class EntityInstanceUpdateRequest
{
    public Guid Id { get; set; }

    public Guid EntityConfigurationId { get; set; }

    public List<AttributeInstanceCreateUpdateRequest> AttributesToAddOrUpdate { get; set; }
    public List<string>? AttributesMachineNamesToRemove { get; set; }
}

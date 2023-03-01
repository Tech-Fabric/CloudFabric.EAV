namespace CloudFabric.EAV.Models.RequestModels
{
    public class EntityInstanceUpdateRequest
    {
        public Guid Id { get; set; }

        public Guid EntityConfigurationId { get; set; }

        public List<AttributeInstanceCreateUpdateRequest> Attributes { get; set; }
    }
}

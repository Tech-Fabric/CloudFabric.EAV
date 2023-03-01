using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class SerialAttributeConfigurationCreateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public long StartingNumber { get; set; }

        public int Increment { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Serial;
    }
}

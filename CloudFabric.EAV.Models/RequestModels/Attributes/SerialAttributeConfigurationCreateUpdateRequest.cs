using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class SerialAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public long StartingNumber;

        public int Increment;

        public override EavAttributeType ValueType { get; } = EavAttributeType.Serial;
    }
}

using Nastolkino.Data.Enums;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class NumberAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public float DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Number;
    }
}
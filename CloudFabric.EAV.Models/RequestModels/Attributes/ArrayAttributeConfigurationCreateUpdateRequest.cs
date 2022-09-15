using Nastolkino.Data.Enums;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class ArrayAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public override EavAttributeType ValueType { get; } = EavAttributeType.Array;

        public EavAttributeType ItemsType { get; set; }

        public AttributeConfigurationCreateUpdateRequest ItemsAttributeConfiguration { get; set; }
    }
}
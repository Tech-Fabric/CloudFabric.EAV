using Nastolkino.Data.Enums;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class TextAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public string DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Text;
    }
}
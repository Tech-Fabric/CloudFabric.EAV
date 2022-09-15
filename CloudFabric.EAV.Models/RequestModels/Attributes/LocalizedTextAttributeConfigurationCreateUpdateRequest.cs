using Nastolkino.Data.Enums;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class LocalizedTextAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public string DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.LocalizedText;
    }
}
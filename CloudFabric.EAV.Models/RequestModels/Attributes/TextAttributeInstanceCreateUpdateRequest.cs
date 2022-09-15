using Nastolkino.Data.Enums;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class TextAttributeInstanceCreateUpdateRequest : AttributeInstanceCreateUpdateRequest
    {
        public string Value { get; set; }
    }
}
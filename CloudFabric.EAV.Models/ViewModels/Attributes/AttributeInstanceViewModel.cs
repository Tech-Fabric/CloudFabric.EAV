using Nastolkino.Json.Utilities;

using System.Text.Json.Serialization;

namespace Nastolkino.Service.Models.ViewModels.EAV.Attributes
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeInstanceViewModel>))]
    public class AttributeInstanceViewModel
    {
    }
}
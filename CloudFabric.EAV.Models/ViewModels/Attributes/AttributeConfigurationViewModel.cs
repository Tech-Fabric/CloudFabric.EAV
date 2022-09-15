using Nastolkino.Data.Enums;
using Nastolkino.Json.Utilities;

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nastolkino.Service.Models.ViewModels.EAV.Attributes
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeConfigurationViewModel>))]
    public abstract class AttributeConfigurationViewModel
    {
        public List<LocalizedStringViewModel> Name { get; set; }

        public List<LocalizedStringViewModel> Description { get; set; }

        public string MachineName { get; set; }

        public EavAttributeType ValueType { get; set; }
    }
}
using Nastolkino.Data.Enums;
using Nastolkino.Service.Models.ViewModels.EAV.Attributes;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class ArrayAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public EavAttributeType ItemsType { get; set; }

        public AttributeConfigurationViewModel ItemsAttributeConfiguration { get; set; }
    }
}
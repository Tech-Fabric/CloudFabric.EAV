using Nastolkino.Json.Utilities;
using Nastolkino.Service.Models.ViewModels.EAV.Attributes;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nastolkino.Service.Models.ViewModels.EAV
{
    public class EntityConfigurationViewModel
    {
        public Guid Id { get; set; }
        
        public List<LocalizedStringViewModel> Name { get; set; }
        
        public string MachineName { get; set; }
        
        public List<AttributeConfigurationViewModel> Attributes { get; set; }
    }
}
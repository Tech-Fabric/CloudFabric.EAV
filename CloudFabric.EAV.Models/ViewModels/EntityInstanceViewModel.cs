using Nastolkino.Service.Models.ViewModels.EAV.Attributes;

using System;
using System.Collections.Generic;


namespace Nastolkino.Service.Models.ViewModels.EAV
{
    public class EntityInstanceViewModel
    {
        public Guid Id { get; set; }
        public Guid EntityConfigurationId { get; set; }
        
        public List<AttributeInstanceViewModel> Attributes { get; set; }
    }
}
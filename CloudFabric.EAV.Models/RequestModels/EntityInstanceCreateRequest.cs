using Nastolkino.Service.Models.RequestModels.EAV.Attributes;

using System;
using System.Collections.Generic;


namespace Nastolkino.Service.Models.RequestModels.EAV
{
    public class EntityInstanceCreateRequest
    {
        public Guid EntityConfigurationId { get; set; }
        
        public List<AttributeInstanceCreateUpdateRequest> Attributes { get; set; }
    }
}
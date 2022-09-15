using Nastolkino.Service.Models.RequestModels.EAV.Attributes;

using System;
using System.Collections.Generic;


namespace Nastolkino.Service.Models.RequestModels.EAV
{
    public class EntityInstanceUpdateRequest
    {
        public Guid Id { get; set; }
        
        public Guid EntityConfigurationId { get; set; }
        
        public List<AttributeInstanceCreateUpdateRequest> Fields { get; set; }
    }
}
namespace CloudFabric.EAV.Models.ViewModels.EAV;
public class HierarchyViewModel
    {
        public Guid Id { get; set; }
        public string MachineName { get; protected set; }
        public Guid EntityConfigurationId { get; protected set; }
    }    

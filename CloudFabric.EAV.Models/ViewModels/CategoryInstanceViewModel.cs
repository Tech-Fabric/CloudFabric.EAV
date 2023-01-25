using System.Collections.ObjectModel;

using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.LocalEventSourcingPackages.ViewModels
{
    public class CategoryInstanceViewModel
    {
        public string PartitionKey { get; set; }
        public Guid EntityConfigurationId { get; protected set; }
        public ReadOnlyCollection<AttributeInstanceViewModel> Attributes { get; protected set; }
        public Guid? TenantId { get; protected set; }
        public string CategoryPath { get; protected set; }
    }
}
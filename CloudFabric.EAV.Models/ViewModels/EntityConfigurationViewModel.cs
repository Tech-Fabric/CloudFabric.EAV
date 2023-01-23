using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Models.ViewModels.Attributes;

public class EntityConfigurationViewModel
{
    public Guid Id { get; set; }

    public List<LocalizedStringViewModel> Name { get; set; }

    public string PartitionKey { get; set; }

    public string MachineName { get; set; }

    public List<EntityConfigurationAttributeReferenceViewModel> Attributes { get; set; }
    
    public Guid? TenantId { get; set; }
}
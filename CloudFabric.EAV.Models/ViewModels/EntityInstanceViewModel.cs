﻿using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.ViewModels;

public class EntityInstanceViewModel
{
    public Guid Id { get; set; }

    public Guid EntityConfigurationId { get; set; }

    public List<AttributeInstanceViewModel> Attributes { get; set; }

    public Guid? TenantId { get; set; }

    public string PartitionKey { get; set; }

    public List<CategoryPathViewModel> CategoryPaths { get; set; }
}

public class EntityTreeInstanceViewModel
{
    public Guid Id { get; set; }

    public string MachineName { get; set; }

    public Guid EntityConfigurationId { get; set; }

    public List<AttributeInstanceViewModel> Attributes { get; set; }

    public Guid? TenantId { get; set; }

    public string PartitionKey { get; set; }

    public List<CategoryPathViewModel> CategoryPaths { get; set; }

    public List<EntityTreeInstanceViewModel> Children { get; set; }
}

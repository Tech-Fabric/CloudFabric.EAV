using System.Collections.ObjectModel;

using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.ViewModels;

public record EntityInstanceViewModel
{
    public Guid Id { get; init; }

    public Guid EntityConfigurationId { get; init; }

    public string? MachineName { get; init; }

    public ReadOnlyCollection<AttributeInstanceViewModel> Attributes { get; init; } =
        new ReadOnlyCollection<AttributeInstanceViewModel>(new List<AttributeInstanceViewModel>());

    public Guid? TenantId { get; init; }

    public string? PartitionKey { get; init; }

    public ReadOnlyCollection<CategoryPathViewModel> CategoryPaths { get; init; } =
        new ReadOnlyCollection<CategoryPathViewModel>(new List<CategoryPathViewModel>());
}

public record EntityTreeInstanceViewModel : EntityInstanceViewModel
{
    public List<EntityTreeInstanceViewModel> Children { get; set; } = new ();
}

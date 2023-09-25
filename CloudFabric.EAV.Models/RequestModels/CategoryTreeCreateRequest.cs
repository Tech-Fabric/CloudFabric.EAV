namespace CloudFabric.EAV.Models.RequestModels;

public class CategoryTreeCreateRequest
{
    /// <summary>
    /// Unique human-readable string identifier.
    /// </summary>
    public string MachineName { get; set; }

    /// <summary>
    /// All tree leaves (categories) within one tree should have same entity configuration id and same
    /// set of attributes.
    /// </summary>
    public Guid EntityConfigurationId { get; set; }

    /// <summary>
    /// Tenant id - just a value for partitioning.
    /// </summary>
    public Guid? TenantId { get; set; }
}

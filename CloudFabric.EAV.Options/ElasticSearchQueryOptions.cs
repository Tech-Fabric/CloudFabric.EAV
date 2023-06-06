namespace CloudFabric.EAV.Options;

/// <summary>
/// Supported max count of returned records.
/// If you have to return more than 10 000 records,
/// take care of implementing SearchAfter() in ElasticSearchProjectionRepository
/// </summary>
public class ElasticSearchQueryOptions
{
    public int MaxSize { get; set; } = 10000;
}

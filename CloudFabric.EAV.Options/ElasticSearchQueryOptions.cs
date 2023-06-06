namespace CloudFabric.EAV.Options;
public class ElasticSearchQueryOptions
{
    // Supported max count of returned records.
    // If you have to return more than 10 000 records,
    // take care of implementing SearchAfter() in ElasticSearchProjectionRepository
    public int MaxSize { get; set; } = 10000;
}

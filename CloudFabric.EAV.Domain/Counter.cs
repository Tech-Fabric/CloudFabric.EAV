namespace CloudFabric.EAV.Domain;

public class Counter
{
    public long NextValue { get; set; }

    private int? _lastIncrement;
    public int? LastIncrement
    {
        get { return _lastIncrement; }
        init { _lastIncrement = value; }
    }

    private DateTime _timestamp;
    public DateTime Timestamp
    {
        get { return _timestamp; }
        init { _timestamp = value; }
    }

    public Guid AttributeConfidurationId { get; init; }

    // Used for Json deserialization
    public Counter()
    {
    }

    public Counter(long nextValue, DateTime timestamp, Guid attributeConfigurationId, int? lastIncrement = null)
    {
        NextValue = nextValue;
        Timestamp = timestamp;
        AttributeConfidurationId = attributeConfigurationId;
        LastIncrement = lastIncrement;
    }

    public void SetTimestamp(DateTime timestamp)
    {
        _timestamp = timestamp;
    }

    public long Step(int increment)
    {
        _lastIncrement = increment;
        return this.NextValue += increment;
    }
}

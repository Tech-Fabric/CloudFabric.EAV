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

    private DateTime _timeStamp;
    public DateTime TimeStamp
    {
        get { return _timeStamp; }
        init { _timeStamp = value; }
    }

    public Guid AttributeConfidurationId { get; init; }

    // Used for Json deserialization
    public Counter()
    {
    }

    public Counter(long nextValue, DateTime timeStamp, Guid attributeConfigurationId, int? lastIncrement = null)
    {
        NextValue = nextValue;
        TimeStamp = timeStamp;
        AttributeConfidurationId = attributeConfigurationId;
        LastIncrement = lastIncrement;
    }

    public void SetTimeStamp(DateTime timeStamp)
    {
        _timeStamp = timeStamp;
    }

    public long Step(int increment)
    {
        _lastIncrement = increment;
        return this.NextValue += increment;
    }
}

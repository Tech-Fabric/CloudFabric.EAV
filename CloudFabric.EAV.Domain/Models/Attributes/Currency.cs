namespace CloudFabric.EAV.Domain.Models.Attributes;

public class Currency
{
    public Currency(string name, string machineName, string prefix)
    {
        Name = name;
        MachineName = machineName;
        Prefix = prefix;
    }

    public string Name { get; set; }
    public string MachineName { get; set; }
    public string Prefix { get; set; }

    protected bool Equals(Currency other)
    {
        return Name == other.Name
               && MachineName == other.MachineName
               && Prefix == other.Prefix;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((Currency)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, MachineName, Prefix);
    }
}

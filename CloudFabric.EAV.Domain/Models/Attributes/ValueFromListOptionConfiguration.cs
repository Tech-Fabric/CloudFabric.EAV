using System.Text.RegularExpressions;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class ValueFromListOptionConfiguration
    {

        public ValueFromListOptionConfiguration(string name, string? machineName)
        {
            Name = name;
            Id = Guid.NewGuid();

            if (string.IsNullOrEmpty(machineName))
            {
                machineName = name.Replace(" ", "_");
                Regex specSymbolsRegex = new Regex("[^\\d\\w_]*", RegexOptions.None, TimeSpan.FromMilliseconds(100));
                machineName = specSymbolsRegex.Replace(machineName, "").ToLower();
            }

            MachineName = machineName;
        }
        public Guid Id { get; }
        public string MachineName { get; private set; }
        public string Name { get; set; }

        protected bool Equals(ValueFromListOptionConfiguration other)
        {
            return Id == other.Id
                   && Name == other.Name
                   && MachineName == other.MachineName;
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
            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ValueFromListOptionConfiguration)obj);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name, MachineName);
        }
    }
}
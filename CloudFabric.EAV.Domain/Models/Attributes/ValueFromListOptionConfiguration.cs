namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class ValueFromListOptionConfiguration
    {

        public ValueFromListOptionConfiguration(string name, string machineName, object? valueToAppend)
        {
            Name = name;
            ValueToAppend = valueToAppend;
            MachineName = machineName;
            Id = Guid.NewGuid();
        }
        public Guid Id { get; }
        public string MachineName { get; set; }
        public string Name { get; set; }
        public object? ValueToAppend { get; set; }

        protected bool Equals(ValueFromListOptionConfiguration other)
        {
            return Id == other.Id
                   && Name == other.Name
                   && MachineName == other.MachineName
                   && Equals(ValueToAppend, other.ValueToAppend);
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
            return HashCode.Combine(Id, Name, ValueToAppend, MachineName);
        }
    }
}
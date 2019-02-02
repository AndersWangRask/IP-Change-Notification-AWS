namespace IPChange.Core.Model
{
    public class MultiClientEntry
    {
        public string Name { get; set; }
        public string IP { get; set; }

        public override string ToString() => $"Name: {Name}, IP: {IP}";
    }
}
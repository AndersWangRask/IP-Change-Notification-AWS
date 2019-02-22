using System;

namespace IPChange.Core.Model
{
    public class MultiClientEntry
    {
        public string Name { get; set; }
        public string IP { get; set; }
        public DateTime UpdatedOnUTC { get; set; }

        public override string ToString() => 
            $"Name: {Name}, IP: {IP}, Updated On: {(UpdatedOnUTC == default ? "" : UpdatedOnUTC.ToString())}";
    }
}
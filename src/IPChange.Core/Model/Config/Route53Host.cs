using System;
using System.Collections.Generic;
using System.Text;

namespace IPChange.Core.Model.Config
{
    /// <summary>
    /// Hold the values of a Route 53 Host
    /// </summary>
    public class Route53Host
    {
        /// <summary>
        /// The Route 53 Zone ID in which to update the entry
        /// </summary>
        public string ZoneId { get; set; }

        /// <summary>
        /// The full DNS name to update
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// TTL (Time To Live) value
        /// </summary>
        public int TTL { get; set; }

        public override string ToString()
        {
            return $"ZoneId: {ZoneId}, Name: {Name}, TTL: {TTL}";
        }
    }
}

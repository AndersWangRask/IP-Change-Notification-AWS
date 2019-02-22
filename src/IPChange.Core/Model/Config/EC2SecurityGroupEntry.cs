using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace IPChange.Core.Model.Config
{
    /// <summary>
    /// An entry in an EC2 Security Group that should be updated with the new IP.
    /// </summary>
    public class EC2SecurityGroupEntry
    {
        public static IEnumerable<string> AllowedIpProtocols { get; } =
            (new List<string>() { "tcp", "udp", "icmp" }).AsReadOnly();

        /// <summary>
        /// The ID of the EC2 Security Group to update
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// The Port Range of the entry in a string format.
        /// Format e.g. "25", "5985-5986".
        /// This will be automatically created if a Port Range object is set.
        /// </summary>
        public string PortRangeString
        {
            get
            {
                return _portRangeString;
            }
            set
            {
                if (_portRangeString != value)
                {
                    _portRangeString = value;
                    _portRange = null;
                }
            }
        }
        private string _portRangeString;

        /// <summary>
        /// The Port Range of the entry as a Port Range object.
        /// This will be automatically created if a Port Range string is set.
        /// </summary>
        public PortRange PortRange
        {
            get
            {
                if (_portRange == null)
                {
                    _portRange = new PortRange(PortRangeString);
                }

                return _portRange;
            }
            set
            {
                _portRange = value;
                _portRangeString = _portRange.ToString();
            }
        }
        private PortRange _portRange;

        public string IpProtocol
        {
            get => _ipProtocol;
            set
            {
                if (string.IsNullOrWhiteSpace(value)
                    ||
                    !AllowedIpProtocols.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    throw new ApplicationException(
                        $"\"{value}\" is not an allowed value for IP Protocol. " +
                        $"Allowed values are \"{string.Join(", ", AllowedIpProtocols)}\"");
                }

                _ipProtocol = value;
            }
        }
        private string _ipProtocol = "tcp";

        public string Description { get; set; }

        public override string ToString() => 
            $"GroupId: {GroupId}, PortRange: {PortRange}, IP Protocol: {IpProtocol}" +
            (string.IsNullOrWhiteSpace(Description) ? "" : ", Description: " + Description);
    }
}

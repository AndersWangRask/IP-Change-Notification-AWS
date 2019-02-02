using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace IPChange.Core.Model.Config
{
    /// <summary>
    /// Used to hold a range of port numbers
    /// </summary>
    public class PortRange
    {
        public int FromPort
        {
            get
            {
                if (_fromPort.HasValue)
                {
                    return _fromPort.Value;
                }
                else
                {
                    return -1;
                }
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException($"Port number must be 0 or positive. Was {value}.");
                }

                _fromPort = value;
            }
        }
        private int? _fromPort = null;

        public int ToPort
        {
            get
            {
                if (_toPort.HasValue)
                {
                    return _toPort.Value;
                }
                else
                {
                    return FromPort;
                }
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException($"Port number must be 0 or positive. Was {value}.");
                }

                if (FromPort > value)
                {
                    throw new ArgumentException($"When ToPort is specified it must be greater than FromPort. Was: FromPort: {FromPort}, ToPort: {value}");
                }

                _toPort = value;
            }
        }
        private int? _toPort = null;

        public PortRange()
        {
            //does nothing
        }

        public PortRange(int FromPort, int? ToPort = null)
        {
            this.FromPort = FromPort;

            if (ToPort.HasValue)
            {
                this.ToPort = ToPort.Value;
            }
        }

        public PortRange(string PortRange)
        {
            if (string.IsNullOrWhiteSpace(PortRange))
            {
                throw new ArgumentException($"PortRange cannot be empty.");
            }

            List<int> candidates = new List<int>();
            foreach (string pr in PortRange.Split("-"[0]))
            {
                if (int.TryParse(pr, out int candidate))
                {
                    candidates.Add(candidate);
                }
            }

            //Order FromPort has to be lowest and ToPort has to be highest
            //(Yeah, well, this could of course ruin the intent of the caller .... but hey!)
            candidates =
                candidates
                    .OrderBy(i => i)
                    .ToList();

            if (candidates.Count > 0) { FromPort = candidates[0]; }
            if (candidates.Count > 1) { ToPort = candidates[1]; }

            //If NEITHER were set
            if (FromPort < 0)
            {
                throw new ArgumentException($"The PortRange Argument Value did not translate into a valid port range: PortRange: {PortRange}");
            }
        }

        public override string ToString()
        {
            if (FromPort < 0 && ToPort < 0)
            {
                return "";
            }

            if (FromPort >= 0 && FromPort == ToPort)
            {
                return FromPort.ToString();
            }

            if (FromPort >= 0 && ToPort >= 0 && FromPort <= ToPort)
            {
                return $"{FromPort}-{ToPort}";
            }

            return $"invalid port range ({FromPort}-{ToPort})";
        }

    }
}

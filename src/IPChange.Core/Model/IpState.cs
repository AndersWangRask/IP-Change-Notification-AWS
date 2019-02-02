using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace IPChange.Core.Model
{
    /// <summary>
    /// Represents the state of a change in external IPv4 address.
    /// SUPPORTS ONLY IPv4 (and not IPv6)
    /// </summary>
    /// <remarks>
    /// Note that IPv6 is not supported simply because currently haven't got a chance to test this through.
    /// And also maybe not the most relevant currently.
    /// </remarks>
    public class IpState
    {
        public IpState(string OldIP, string NewIP, bool ForceUpdate = false)
        {
            this.OldIP = OldIP;
            this.NewIP = NewIP;

            this.ForceUpdate = ForceUpdate;
        }

        /// <summary>
        /// Old (previous) IP address
        /// </summary>
        public string OldIP
        {
            get => _oldIP?.ToString();
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _oldIP = null;
                }
                else if (!IPAddress.TryParse(value, out _oldIP) || _oldIP.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    throw new ArgumentException($"The {nameof(OldIP)} input value \"{value}\" is not a valid IPv4 address.");
                }
            }
        }
        private IPAddress _oldIP;

        /// <summary>
        /// New (current) IP address
        /// </summary>
        public string NewIP
        {
            get => _newIP?.ToString();
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _newIP = null;
                }
                else if (!IPAddress.TryParse(value, out _newIP) || _newIP.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    throw new ArgumentException($"The {nameof(NewIP)} input value \"{value}\" is not a valid IPv4 address.");
                }
            }
        }
        private IPAddress _newIP;

        /// <summary>
        /// Whether to Force an Update through the system even if the IP address has not changed.
        /// </summary>
        public bool ForceUpdate { get; set; }

        /// <summary>
        /// Old (previous) IP address expressed as an IPv4 range (e.g. "192.168.1.1/32")
        /// </summary>
        /// <remarks>
        /// As this state only deals with single IP addresses and not actually ranges,
        /// this simply postfixes "/32" to the end of it.
        /// </remarks>
        public string OldIPRange => (string.IsNullOrWhiteSpace(OldIP)) ? null : OldIP + "/32";

        /// <summary>
        /// New (current) IP address expressed as an IPv4 range (e.g. "192.168.1.2/32")
        /// </summary>
        /// <remarks>
        /// As this state only deals with single IP addresses and not actually ranges,
        /// this simply postfixes "/32" to the end of it.
        /// </remarks>
        public string NewIPRange => (string.IsNullOrWhiteSpace(NewIP)) ? null : NewIP + "/32";

        /// <summary>
        /// Whether there is a change from old IP to new IP.
        /// </summary>
        public bool Changed => (NewIP != OldIP);

        /// <summary>
        /// Whether there is an old IP address.
        /// </summary>
        /// <remarks>
        /// The very first time the program runs, there will be no old IP.
        /// </remarks>
        public bool HasOldIP => (!string.IsNullOrWhiteSpace(OldIP));

        public override string ToString()
            => $"Old IP: {OldIP}, New IP: {NewIP}, Changed: {Changed}, Force Update: {ForceUpdate}";
    }
}
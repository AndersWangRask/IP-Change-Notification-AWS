using IPChange.Core.Model;
using IPChange.Core.Model.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace IPChange.Core
{
    /// <summary>
    /// The IP Change Worker checks if IP has changed since last,
    /// and if the IP has changed calls the configured workers.
    /// Any new IP address is written in the log.
    /// </summary>
    public class IpChangeWorker : BaseAwsWorker
    {
        private string filePathForIpLogXml
        {
            get => _filePathForIpLogXml;
            set
            {
                _filePathForIpLogXml = value;

                if (!File.Exists(_filePathForIpLogXml))
                {
                    FileInfo fileInfo = new FileInfo(_filePathForIpLogXml);
                    DirectoryInfo directoryInfo = fileInfo.Directory;

                    if (!directoryInfo.Exists)
                    {
                        throw new ApplicationException($"");
                    }
                }

                _ipLogXml = null;
            }
        }
        private string _filePathForIpLogXml;

        private XDocument ipLogXml
        {
            get
            {
                if (_ipLogXml == null)
                {
                    _ipLogXml = XDocument.Load(filePathForIpLogXml);
                }

                return _ipLogXml;

            }
        }
        private XDocument _ipLogXml;

        public IpChangeWorker(Config config, string FilePathForIpLogXml, Action<string> output, MultiClientState multiClientState) : base(config, null, output, multiClientState)
        {
            //Validate
            if (string.IsNullOrWhiteSpace(FilePathForIpLogXml))
            {
                throw new ApplicationException("Must provide a valid File Path for IP Log XML file.");
            }

            FilePathForIpLogXml = FilePathForIpLogXml.Trim();

            if (!File.Exists(FilePathForIpLogXml))
            {
                throw new FileNotFoundException($"The file \"{ FilePathForIpLogXml }\" does exist or access is not allowed.", FilePathForIpLogXml);
            }

            //Load in the data/log file (if any)
            filePathForIpLogXml = FilePathForIpLogXml;
        }

        private string getOldIp()
        {
            //reading the old IP out of the log file
            string oldIp =
                ipLogXml
                    ?.Element("ClientToAWS-IPLog")
                    ?.Element("currentIp")
                    ?.Value;

            return oldIp;
        }

        private void setNewIP(string newIp)
        {
            //Set the New IP
            ipLogXml
                .Element("ClientToAWS-IPLog")
                .Element("currentIp")
                .Value = newIp;

            //Set the DateTime of the update
            ipLogXml
                .Element("ClientToAWS-IPLog")
                .Element("currentIp")
                .Attribute("updated")
                .Value = DateTime.Now.ToUniversalTime().ToString();

            //Add this update to the log of updates
            ipLogXml
                .Element("ClientToAWS-IPLog")
                .Element("logEntries")
                .Add(new XElement("log", new object[] { new XAttribute("updated", DateTime.Now.ToUniversalTime().ToString()), newIp }));

            //Note that the XML file will be saved later
        }

        /// <summary>
        /// Checks if IP address has changed,
        /// and if it has run configured work items
        /// </summary>
        /// <param name="ForceUpdate">
        /// (Optional) Whether to "Force" an update.
        /// If True, the work items will be run even if the IP address is not detected to have changed.
        /// </param>
        /// <returns>
        /// Whether an update took place.
        /// I.e. either the IP address changed or an update was forced,
        /// and the work items was run.
        /// </returns>
        public bool Run(bool ForceUpdate = false)
        {
            //1: Get Old External IP and Current External IP and compare them
            string oldIp = getOldIp();
            string currentIp = Helpers.ExternalIp.GetExternalIpAddress();

            if (!ForceUpdate && currentIp == oldIp)
            {
                //The External IP remains unchanged (and the update is not being forced),
                Output($"No change has taken place. IP is still {oldIp}");

                //Return that no update has taken place
                return false;
            }

            //OUTPUT
            Output($"The new IP is: {currentIp}. Old IP was: {oldIp}");

            //2: If External IP has changed, Run with new update
            //Set the new IP State
            IpState = new IpState(oldIp, currentIp, ForceUpdate);

            //Update IP Log XML
            setNewIP(currentIp);

            //Run through the configured work items
            Run(IpState);

            //3: Savig process output (if any)
            ipLogXml.Save(filePathForIpLogXml);

            //4: return that an update has taken place
            return true;
        }

        /// <summary>
        /// Run configured work items with the new IP address.
        /// </summary>
        /// <param name="IpState">Data on the new IP address.</param>
        public void Run(IpState IpState)
        {
            //Output **START**
            Output($"IP CHANGE: START: IP State: {IpState}");

            //Multi-Client State
            MultiClientState multiClientState = null;

            if (Config.MultiClientSettings != null)
            {
                multiClientState = new MultiClientState(Config, IpState, Output);
                multiClientState.Run();
            }

            //Run Route 53 updates
            if (Config.Route53Hosts != null && Config.Route53Hosts.Any())
            {
                using (Route53Worker route53Worker = new Route53Worker(Config, IpState, Output, multiClientState))
                {
                    route53Worker.RunAll();
                }
            }

            //Security Group (Firewall) updates
            if (Config.EC2SecurityGroupEntries != null && Config.EC2SecurityGroupEntries.Any())
            {
                using (EC2SecurityGroupWorker ec2SecurityGroupWorker = new EC2SecurityGroupWorker(Config, IpState, Output, multiClientState))
                {
                    ec2SecurityGroupWorker.RunAll();
                }
            }

            //OUTPUT
            Output($"IP CHANGE: COMPLETE: IP State: {IpState}");

            //Notifications
            if (Config.NotificationSettings != null && Config.NotificationSettings.Recipients != null && Config.NotificationSettings.Recipients.Any())
            {
                using (NotificationWorker notificationWorker = new NotificationWorker(Config, IpState, Output, _outputLog, multiClientState))
                {
                    notificationWorker.RunAll();
                }
            }

            //Dispose of MultiClientState
            multiClientState?.Dispose();
            multiClientState = null;

            //OUTPUT **FIN**
            Output($"IP CHANGE: Notifications complete. ALL DONE :-)! IP State: {IpState}");
        }

        public override string ToString() => $"IP Change Worker: {IpState}";
    }
}

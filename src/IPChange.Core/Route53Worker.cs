using Amazon;
using Amazon.Route53;
using Amazon.Route53.Model;
using IPChange.Core.Model;
using IPChange.Core.Model.Config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IPChange.Core
{
    /// <summary>
    /// The Route 53 Worker creates/updates hosts in Rout 53.
    /// </summary>
    public class Route53Worker : BaseAwsWorker
    {
        public Route53Worker(Config config, IpState ipState, Action<string> output, MultiClientState multiClientState) : base(config, ipState, output, multiClientState)
        {

        }

        /// <summary>
        /// Will run all the items passed in with the config
        /// </summary>
        /// <returns>
        /// The number of items processed
        /// </returns>
        public int RunAll() => RunItems(Config.Route53Hosts);

        /// <summary>
        /// Will run specific items
        /// </summary>
        /// <param name="items">The items to run</param>
        /// <returns>
        /// The number of items processed
        /// </returns>
        public int RunItems(IEnumerable<Route53Host> route53Hosts)
        {
            int completedItems = 0;

            foreach (Route53Host R53H in route53Hosts)
            {
                RunItem(R53H);
                completedItems++;
            }

            return completedItems;
        }

        /// <summary>
        /// Run a single item
        /// </summary>
        /// <param name="item">The item to run</param>
        public void RunItem(Route53Host item)
        {
            //Output
            Output($"R53: Starting item: {item}, new IP address: {IpState.NewIP}");

            //New the Route 53 Client
            AmazonRoute53Client r53Client =
                new AmazonRoute53Client(
                    Config.BaseSettings.AWSAccessKeyID,
                    Config.BaseSettings.AWSSecretAccessKey,
                    new AmazonRoute53Config { RegionEndpoint = RegionEndpoint });

            //New the Change Record to push
            Change change =
                new Change
                {
                    Action = ChangeAction.UPSERT, //Insert or Update
                    ResourceRecordSet =
                        new ResourceRecordSet
                        {
                            Name = item.Name,
                            TTL = item.TTL,
                            Type = RRType.A,
                            ResourceRecords = new List<ResourceRecord> { new ResourceRecord(IpState.NewIP) }
                        }
                };

            //New the Change Request
            ChangeResourceRecordSetsRequest recordSetsRequest =
                new ChangeResourceRecordSetsRequest
                {
                    HostedZoneId = item.ZoneId,
                    ChangeBatch = new ChangeBatch(new List<Change> { change })
                };

            //Submitting the Change Request to the API and receiving back the ID
            ChangeResourceRecordSetsResponse recordSetResponse = r53Client.ChangeResourceRecordSetsAsync(recordSetsRequest).GetAwaiter().GetResult();

            //The ID of the response
            string changeId = recordSetResponse.ChangeInfo.Id;

            //Output
            Output($"R53: Change ID \"{changeId}\": ChangeRequest was submitted.");

            //Enquire with R53 the status of the change (R53 updates can be VERY slow business)
            GetChangeRequest changeRequest =
                new GetChangeRequest
                {
                    Id = changeId
                };

            while (r53Client.GetChangeAsync(changeRequest).GetAwaiter().GetResult().ChangeInfo.Status == ChangeStatus.PENDING)
            {
                //Output
                Output($"R53: Change ID \"{changeId}\": Change is still pending. (Can take a while.)");

                //Wait
                Thread.Sleep(10 * 1000); //Wait ten seconds
            }

            //Output DONE
            Output($"R53: Change ID \"{changeId}\": Change IN SYNC. Done.");
        }

        public override string ToString() => $"AWS Route53 Worker: {IpState}";
    }
}
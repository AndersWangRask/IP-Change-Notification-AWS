using Amazon;
using Amazon.Route53;
using Amazon.Route53.Model;
using IPChange.Core.Helpers;
using IPChange.Core.Model;
using IPChange.Core.Model.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IPChange.Core
{
    public class MultiClientState : BaseAwsWorker
    {
        public MultiClientState(Config config, IpState ipState, Action<string> output) : base(config, ipState, output, null)
        {

        }

        public List<MultiClientEntry> GetClients()
        {
            //Output
            Output($"MCS: Starting Get Clients");

            //1) Get the current data-text from R53
            //New the Route 53 Client
            AmazonRoute53Client r53Client =
                new AmazonRoute53Client(
                    Config.BaseSettings.AWSAccessKeyID,
                    Config.BaseSettings.AWSSecretAccessKey,
                    new AmazonRoute53Config
                    {
                        RegionEndpoint = RegionEndpoint
                    });

            //Define the request
            ListResourceRecordSetsRequest listResourceRecordSetsRequest =
                new ListResourceRecordSetsRequest
                {
                    HostedZoneId = Config.MultiClientSettings.Route53.R53ZoneId,
                    MaxItems = 1.ToString(),
                    StartRecordName = Config.MultiClientSettings.Route53.Name
                };

            //Get the response
            ListResourceRecordSetsResponse listResourceRecordSetsResponse =
                r53Client
                    .ListResourceRecordSetsAsync(listResourceRecordSetsRequest)
                    .GetAwaiter()
                    .GetResult();

            //Get the Data Text (if any)
            //listResourceRecordSetsResponse.ResourceRecordSets[0].ResourceRecords[0].Value
            string dataText =
                listResourceRecordSetsResponse
                    .ResourceRecordSets?
                    .Where(rrsi => string.Equals(rrsi.Name, Config.MultiClientSettings.Route53.Name, StringComparison.InvariantCultureIgnoreCase))
                    .FirstOrDefault()?
                    .ResourceRecords?
                    .FirstOrDefault()?
                    .Value;

            if (string.IsNullOrWhiteSpace(dataText))
            {
                //Since there somehow were no existing data in R53
                //We just return an empty list

                //Output
                Output($"MCS: Found no existing record");

                //return
                return new List<MultiClientEntry>();
            }

            //2) Decode the data string to a JSON string (decompress, decrypt)
            //Remove any "
            if (dataText.StartsWith("\"") && dataText.EndsWith("\""))
            {
                dataText = dataText.Substring(1, dataText.Length - 2);
            }

            //Decompress
            dataText = dataText.Decompress();

            //Decrypt
            dataText = dataText.Decrypt(Config.MultiClientSettings.Route53.EncryptionPassword);

            //3) De-serialize the JSON to the return value
            List<MultiClientEntry> multiClientEntries =
                JsonConvert.DeserializeObject<List<MultiClientEntry>>(dataText);

            //Output
            Output($"MCS: Completed Get Clients. Got {multiClientEntries.Count}");

            //return
            return multiClientEntries;
        }

        public void SaveClients()
        {
            //Output
            Output($"MCS: Starting Save Clients");

            string dataText = "";

            //Validate
            if (Clients == null || !Clients.Any())
            {
                //Output
                Output($"MCS: Had no Clients.");

                //Will update with an empty string - no clients
            }
            else
            {
                //Output
                Output($"MCS: Found {Clients.Count} Clients.");

                //1) Serialize to JSON text
                dataText = JsonConvert.SerializeObject(Clients);

                //2) Prepare the string (encrypt, compress)
                //Encrypt
                dataText = dataText.Encrypt(Config.MultiClientSettings.Route53.EncryptionPassword);

                //Compress
                dataText = dataText.Compress();
            }

            //Add quotation marks
            dataText = "\"" + dataText + "\"";

            //3) Update to R53
            //New the Route 53 Client
            AmazonRoute53Client r53Client =
                new AmazonRoute53Client(
                    Config.BaseSettings.AWSAccessKeyID,
                    Config.BaseSettings.AWSSecretAccessKey,
                    new AmazonRoute53Config
                    {
                        RegionEndpoint = RegionEndpoint
                    });

            //New the Change Record to push
            Change change =
                new Change
                {
                    Action = ChangeAction.UPSERT, //Insert or Update
                    ResourceRecordSet =
                        new ResourceRecordSet
                        {
                            Name = Config.MultiClientSettings.Route53.Name,
                            TTL = 300,
                            Type = RRType.TXT,
                            ResourceRecords = new List<ResourceRecord> { new ResourceRecord(dataText) }
                        }
                };

            //New the Change Request
            ChangeResourceRecordSetsRequest recordSetsRequest =
                new ChangeResourceRecordSetsRequest
                {
                    HostedZoneId = Config.MultiClientSettings.Route53.R53ZoneId,
                    ChangeBatch = new ChangeBatch(new List<Change> { change })
                };

            //Submitting the Change Request to the API and receiving back the ID
            ChangeResourceRecordSetsResponse recordSetResponse =
                r53Client
                    .ChangeResourceRecordSetsAsync(recordSetsRequest)
                    .GetAwaiter()
                    .GetResult();

            //The ID of the response
            string changeId = recordSetResponse.ChangeInfo.Id;

            //Output
            Output($"MCS-R53: Change ID \"{changeId}\": ChangeRequest was submitted.");

            //Enquire with R53 the status of the change (R53 updates can be VERY slow business)
            GetChangeRequest changeRequest =
                new GetChangeRequest
                {
                    Id = changeId
                };

            while (r53Client.GetChangeAsync(changeRequest).GetAwaiter().GetResult().ChangeInfo.Status == ChangeStatus.PENDING)
            {
                //Output
                Output($"MCS-R53: Change ID \"{changeId}\": Change is still pending. (Can take a while.)");

                //Wait
                Thread.Sleep(10 * 1000); //Wait ten seconds
            }

            //Output DONE
            Output($"MCS-R53: Change ID \"{changeId}\": Change IN SYNC. Done.");
            Output($"MCS: Completed Save of {Clients.Count} Clients");
        }

        public List<MultiClientEntry> Clients
        {
            get
            {
                if (_clients == null)
                {
                    _clients = GetClients();
                }

                return _clients;
            }
        }
        private List<MultiClientEntry> _clients = null;

        public void Run()
        {
            MultiClientEntry item =
                Clients
                    .Where(mcei => string.Equals(Config.MultiClientSettings.ClientName, mcei.Name, StringComparison.InvariantCultureIgnoreCase))
                    .FirstOrDefault();

            if (item == null)
            {
                item = new MultiClientEntry();
                item.Name = Config.MultiClientSettings.ClientName;

                Clients.Add(item);

                //Output
                Output($"MCS: Created new Multi Client Entry: {item}");
            }
            else
            {
                //Output
                Output($"MCS: Found existing Multi Client Entry: {item}");
            }

            item.IP = IpState.NewIP;
            item.UpdatedOnUTC = DateTime.Now.ToUniversalTime();

            //Save Clients
            SaveClients();
        }
    }
}
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using IPChange.Core.Model;
using IPChange.Core.Model.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;


namespace IPChange.Core
{
    internal class EC2SecurityGroupWorker : BaseAwsWorker
    {
        public EC2SecurityGroupWorker(Config config, IpState ipState, Action<string> output, MultiClientState multiClientState) : base(config, ipState, output, multiClientState)
        {

        }

        /// <summary>
        /// Will run all the items passed in with the config
        /// </summary>
        /// <returns>
        /// The number of items processed
        /// </returns>
        public int RunAll() => RunItems(Config.EC2SecurityGroupEntries);

        /// <summary>
        /// Will run specific items
        /// </summary>
        /// <param name="items">The items to run</param>
        /// <returns>
        /// The number of items processed
        /// </returns>
        public int RunItems(IEnumerable<EC2SecurityGroupEntry> items)
        {
            int completedItems = 0;

            foreach (EC2SecurityGroupEntry item in items)
            {
                RunItem(item);
                completedItems++;
            }

            return completedItems;
        }

        /// <summary>
        /// Run a single item
        /// </summary>
        /// <param name="item">The item to run</param>
        public void RunItem(EC2SecurityGroupEntry item)
        {
            AmazonEC2Client ec2Client =
                new AmazonEC2Client(
                    Config.BaseSettings.AWSAccessKeyID,
                    Config.BaseSettings.AWSSecretAccessKey,
                    new AmazonEC2Config
                    {
                        RegionEndpoint = RegionEndpoint
                    });

            //1) Revoke Old entries/permissions
            if (IpState.HasOldIP && IpState.Changed)
            {
                bool oldIpStillInUse =
                    MultiClientState?
                        .Clients?
                        .Where(mcei => mcei.IP == IpState.OldIP)
                        .Any()
                    ??
                    false;

                if (!oldIpStillInUse)
                {
                    RevokeSecurityGroupIngressRequest revokeRequest =
                        new RevokeSecurityGroupIngressRequest
                        {
                            GroupId = item.GroupId,
                            IpPermissions =
                                new List<IpPermission>
                                {
                                    new IpPermission
                                    {
                                        IpProtocol = item.IpProtocol,
                                        FromPort = item.PortRange.FromPort,
                                        ToPort = item.PortRange.ToPort,
                                        Ipv4Ranges =
                                            new List<IpRange>
                                            {
                                                new IpRange
                                                {
                                                    CidrIp = IpState.NewIPRange
                                                }
                                            }
                                    }
                                }
                        };

                    RevokeSecurityGroupIngressResponse revokeResponse = null;

                    try
                    {
                        revokeResponse =
                            ec2Client
                                .RevokeSecurityGroupIngressAsync(revokeRequest)
                                .GetAwaiter()
                                .GetResult();

                        //Output
                        Output($"EC2SG: Revoked Security Group Rule: {item} for Old IP: {IpState.OldIP}");
                    }
                    catch (Exception ex)
                    {
                        //In this case we do nothing.
                        //The old rule was not removed for whatever reason, but we choose to do nothing here
                        //so that the new rules continue to be added.
                        Output($"EC2SG: [ACTION REQUIRED] An exception was thrown while trying to revoke Security Group Rule: {item} for Old IP: {IpState.OldIP}\nRule was likely not revoked.\nException was: {ex}");
                    }
                }
            }

            //2) Insert new entries/permissions
            AuthorizeSecurityGroupIngressRequest authorizeRequest =
                new AuthorizeSecurityGroupIngressRequest
                {
                    GroupId = item.GroupId,
                    IpPermissions =
                        new List<IpPermission>
                        {
                            new IpPermission
                            {
                                IpProtocol = "tcp",
                                FromPort = item.PortRange.FromPort,
                                ToPort = item.PortRange.ToPort,
                                Ipv4Ranges =
                                    new List<IpRange>
                                    {
                                        new IpRange
                                        {
                                            CidrIp = IpState.NewIPRange
                                        }
                                    }
                            }
                        }
                };

            AuthorizeSecurityGroupIngressResponse authorizeResponse = null;

            try
            {
                //Output
                Output($"EC2SG: START   : Adding Security Group Rule: {item} for New IP: {IpState.NewIP}");

                //Send 
                authorizeResponse =
                    ec2Client
                        .AuthorizeSecurityGroupIngressAsync(authorizeRequest)
                        .GetAwaiter()
                        .GetResult();
            }
            catch (AmazonEC2Exception ex)
            {
                //Diagnostics
                Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}: EXCEPTION: \n" + ex.ToString());

                //Handle
                if (string.Equals(ex.ErrorCode, "InvalidPermission.Duplicate", StringComparison.InvariantCultureIgnoreCase))
                {
                    //Apparently it is already there, and we're perfectly happy with that
                    Output($"EC2SG: ERROR   : The entry: {item} for New IP: {IpState.NewIP} was already present. No entry was added.");
                }
                else
                {
                    //Don't really know quite exactly what went wrong :-|
                    Output($"EC2SG: ERROR   : While adding the entry: {item} for New IP: {IpState.NewIP} an exception was thrown. Error Code: {ex.ErrorCode}, Message: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                //Diagnostics
                Debug.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}: EXCEPTION: \n" + ex.ToString());

                //Throw
                throw new ApplicationException("EC2SG: An unknown exception was thrown while trying to update AWS EC2 Security Group", ex);
            }
            finally
            {
                //Output
                Output($"EC2SG: COMPLETE: Finished adding Security Group Rule: {item} for New IP: {IpState.NewIP}");
            }
        }

        public override string ToString() => $"AWS EC2 Security Group Worker: {IpState}";
    }
}
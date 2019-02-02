using System;
using System.Collections.Generic;
using System.Text;

namespace IPChange.Core.Model.Config
{
    /// <summary>
    /// Configuration for the changes to perform and the account and region to do it to
    /// </summary>
    public class Config
    {
        public Config()
        {
            
        }

        /// <summary>
        /// AWS Account Base Settings and Region
        /// </summary>
        public BaseSettings BaseSettings { get; set; }

        /// <summary>
        /// Route53 Hosts to change/update
        /// </summary>
        public IEnumerable<Route53Host> Route53Hosts { get; set; }

        /// <summary>
        /// EC2 Security Group Entries to change/update
        /// </summary>
        public IEnumerable<EC2SecurityGroupEntry> EC2SecurityGroupEntries { get; set; }

        /// <summary>
        /// Settings for sending out notification of the IP address update
        /// </summary>
        public NotificationSettings NotificationSettings { get; set; }

        /// <summary>
        /// Settings for being able to consider multiple clients
        /// </summary>
        public MultiClientSettings MultiClientSettings { get; set; }
    }
}
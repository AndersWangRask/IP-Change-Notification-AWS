using System;
using System.Collections.Generic;
using System.Text;

namespace IPChange.Core.Model.Config
{
    /// <summary>
    /// Base Settings for the AWS Account
    /// </summary>
    /// <remarks>
    /// Only a single Region is supported which has to go here.
    /// If operations in several regions is to be performed, 
    /// two or more different config files is required and then run the program twice or more,
    /// with thos config files.
    /// </remarks>
    public class BaseSettings
    {
        /// <summary>
        /// The Access Key ID for the AWS account
        /// </summary>
        public string AWSAccessKeyID { get; set; }

        /// <summary>
        /// The Secret Access Key for the AWS account
        /// </summary>
        public string AWSSecretAccessKey { get; set; }

        /// <summary>
        /// The AWS Region to perform the changes in.
        /// Refer to AWS documentation for lists of regions.
        /// </summary>
        /// <remarks>
        /// See here: https://docs.aws.amazon.com/general/latest/gr/rande.html
        /// Use the "eu-west-1" format.
        /// </remarks>
        public string AWSRegion { get; set; }

        public override string ToString() => $"AWS Config Base Settings: Region: {AWSRegion}";
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Diagnostics;

namespace IPChange.Core.Helpers
{
    public static class ExternalIp
    {
        public const string URL_GET_EXTERNAL_IP = "http://checkip.amazonaws.com/";

        public static string GetExternalIpAddress()
        {
            string ip = "";

            //Use an external endpoint to get the external IP address
            try
            {
                using (WebClient wc = new WebClient())
                {
                    ip = wc.DownloadString(URL_GET_EXTERNAL_IP);
                }

                Debug.WriteLine($"External IP message: {ip}");
            }
            catch (Exception e)
            {
                throw new ApplicationException($"While trying to download URL \"{URL_GET_EXTERNAL_IP}\" an exception was thrown. The exception is in the data.", e);
            }

            //Validate the returned value
            if (string.IsNullOrWhiteSpace(ip))
            {
                throw new ApplicationException($"The URL \"{URL_GET_EXTERNAL_IP}\" seemed to return a blank string and not the External IP address as expected.");
            }

            //Replace new lines and spaces with blank plus trim
            //(**COULD** in some cases cause issues depending on the full string content)
            ip = ip.Replace("\n", "").Replace(" ", "").Trim();

            //Regex to validate/capture IP address from string
            //Source: https://stackoverflow.com/questions/4890789/regex-for-an-ip-address 
            Regex ipRegex = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
            MatchCollection ipRegexResult = ipRegex.Matches(ip);

            //Check if there is ANY capture, which would indicate that there is an IP address
            //If there is an IP address, we are **ASSUMING** that to be the valid external IP, given the public endpoint
            if (ipRegexResult.Count > 0)
            {
                //Set the IP to the captured output
                ip = ipRegexResult[0].Value;
            }
            else
            {
                throw new ApplicationException($"The URL \"{URL_GET_EXTERNAL_IP}\" returned content that did not seem to be a valid IP address.");
            }

            //RETURN the IP address
            return ip;
        }
    }
}

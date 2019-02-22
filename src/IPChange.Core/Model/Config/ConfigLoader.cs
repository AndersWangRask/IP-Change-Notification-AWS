using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace IPChange.Core.Model.Config
{
    /// <summary>
    /// Loads Configuration from XML work file
    /// </summary>
    public static class ConfigLoader
    {
        /// <summary>
        /// Loads Configuration from XML work file given the file path of the xml file
        /// </summary>
        /// <param name="XmlFilePath">The file path of the XML file</param>
        /// <returns>
        /// A Configuration object based on the XML file
        /// </returns>
        public static Config LoadConfigFromXml(string XmlFilePath)
        {
            //Validate
            if (string.IsNullOrWhiteSpace(XmlFilePath))
            {
                throw new ApplicationException("Must pass in a valid XML File Path");
            }

            XmlFilePath = XmlFilePath.Trim();

            if (!File.Exists(XmlFilePath))
            {
                throw new FileNotFoundException($"XML File at Path \"{XmlFilePath}\" was not found.");
            }

            //Go
            try
            {
                XDocument xml = XDocument.Load(XmlFilePath);
                Config config = LoadConfigFromXml(xml);

                return config;
            }
            catch (Exception ex)
            {
                string msg = $"While trying to Load Config from XML File at Path \"{XmlFilePath}\" an exception was thrown.";

                throw new ApplicationException(msg, ex);
            }
        }

        /// <summary>
        /// Loads Configuration from XML document (XDocument)
        /// </summary>
        /// <param name="Xml">The XDocument holding the configuration/work file</param>
        /// <returns>
        /// A Configuration object based on the XML Document
        /// </returns>
        public static Config LoadConfigFromXml(XDocument Xml)
        {
            //Get and Validate Root and Base Settings
            XElement rootXml = Xml.Element("ClientToAWS-config");
            XElement baseSettingsXml = rootXml?.Element("BaseSettings");

            if (rootXml == null || baseSettingsXml == null)
            {
                throw new ApplicationException("The XML does not appear to be a valid configuration file.");
            }

            if (string.IsNullOrWhiteSpace(baseSettingsXml.Element("AWSAccessKeyID")?.Value)
                ||
                string.IsNullOrWhiteSpace(baseSettingsXml.Element("AWSSecretAccessKey")?.Value)
                ||
                string.IsNullOrWhiteSpace(baseSettingsXml.Element("AWSRegion")?.Value))
            {
                throw new InvalidOperationException("The AWS Config BaseSettings must have valid values for the fields AWSAccessKeyID, AWSSecretAccessKey, and AWSRegion");
            }

            //New the return value Config object
            Config config = new Config
            {
                //Base settings
                BaseSettings = new BaseSettings
                {
                    AWSAccessKeyID = baseSettingsXml.Element("AWSAccessKeyID").Value,
                    AWSSecretAccessKey = baseSettingsXml.Element("AWSSecretAccessKey").Value,
                    AWSRegion = baseSettingsXml.Element("AWSRegion").Value
                },

                //Route53
                Route53Hosts =
                    rootXml.Element("Route53")?.Elements("host")
                        .Where(xi => xi.Element("name") != null && xi.Element("R53ZoneId") != null)
                        .Select(
                            xi =>
                                new Route53Host
                                {
                                    Name = xi.Element("name").Value,
                                    TTL = int.TryParse(xi.Element("name").Attribute("ttl")?.Value, out int ttlCandidate) ? ttlCandidate : 60,
                                    ZoneId = xi.Element("R53ZoneId").Value
                                })
                        .ToList(),

                //EC2SecurityGroup
                EC2SecurityGroupEntries =
                    rootXml.Element("EC2SecurityGroup")?.Elements("entry")
                        .Select(
                            xi =>
                                new EC2SecurityGroupEntry
                                {
                                    GroupId = xi.Element("groupId").Value,
                                    PortRangeString = xi.Element("portRange").Value,
                                    IpProtocol = xi.Element("ipProtocol")?.Value ?? "tcp",
                                    Description = xi.Element("Description")?.Value
                                })
                        .ToList(),

                //Notification
                NotificationSettings = NotificationSettingsLoader.LoadFromXml(rootXml.Element("Notification")),

                //Multi-Client
                MultiClientSettings = MultiClientSettingsLoader.LoadFromXml(rootXml.Element("multiClientState"))
            };

            //Return Value
            return config;
        }
    }
}
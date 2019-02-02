using System;
using System.Xml.Linq;

namespace IPChange.Core.Model.Config
{
    public class MultiClientSettingsLoader
    {
        public static MultiClientSettings LoadFromXml(XElement MultiClientXml)
        {
            //Validate
            if (MultiClientXml == null)
            {
                throw new ArgumentNullException($"{nameof(MultiClientXml)} cannot be null.");
            }

            string clientName = MultiClientXml.Element("clientName")?.Value;

            if (string.IsNullOrWhiteSpace(clientName))
            {
                throw new ArgumentNullException("Client Name must not be blank.");
            }

            XElement r53Xml = MultiClientXml.Element("Route53");

            if (r53Xml == null)
            {
                throw new ArgumentNullException("The Multi-Client State must have a Route53 configuration section. (Since that is the only supported provider.)");
            }

            //New the return value
            MultiClientSettings multiClientSettings =
                new MultiClientSettings
                {
                    ClientName = clientName,
                    Route53 =
                        new MultiClientRoute53ProviderSettings
                        {
                            R53ZoneId = r53Xml.Element("R53ZoneId")?.Value,
                            Name = r53Xml.Element("name")?.Value,
                            UseEncryption = r53Xml.Element("useEncryption")?.Value?.ToLowerInvariant() == "true",
                            EncryptionPassword = r53Xml.Element("encryptionPassword")?.Value
                        }
                };

            //Let's validate some more
            if (string.IsNullOrWhiteSpace(multiClientSettings.Route53.R53ZoneId))
            {
                throw new ArgumentNullException("Multi-Client State Route53 need to set the Zone ID.");
            }

            if (string.IsNullOrWhiteSpace(multiClientSettings.Route53.Name))
            {
                throw new ArgumentNullException("Multi-Client State Route53 need to set the name for the entry.");
            }

            if (multiClientSettings.Route53.UseEncryption && string.IsNullOrWhiteSpace(multiClientSettings.Route53.EncryptionPassword))
            {
                throw new ArgumentNullException("Multi-Client State Route53 must set an Encryption Password when Use Encryption is set to true.");
            }

            //Return
            return multiClientSettings;
        }
    }
}

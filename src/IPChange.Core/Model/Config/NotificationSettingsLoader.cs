using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace IPChange.Core.Model.Config
{
    /// <summary>
    /// Loads Notification Settings Config
    /// </summary>
    public static class NotificationSettingsLoader
    {
        /// <summary>
        /// Loads Notification Settings Config from XML
        /// </summary>
        /// <param name="NotificationXml">The XML</param>
        /// <returns>The Notification Settings Config object</returns>
        public static NotificationSettings LoadFromXml(XElement NotificationXml)
        {
            //Email Config
            XElement emailConfigXml = NotificationXml?.Element("emailConfig");
            XElement recipientsXml = NotificationXml?.Element("recipients");

            if (NotificationXml == null || (emailConfigXml == null && recipientsXml == null))
            {
                return null;
            }

            if (emailConfigXml == null)
            {
                throw new ArgumentNullException("NotificationXml has no email element which is required.");
            }

            //New Notification Settings
            NotificationSettings notificationSettings =
                new NotificationSettings
                {
                    EmailConfig =
                        new NotificationsEmailConfig
                        {
                            SmtpServer = emailConfigXml.Element("smtpServer").Value,
                            SmtpServerPort =
                                int.TryParse(emailConfigXml.Element("smtpServerPort")?.Value, out int candidate)
                                    ?
                                    candidate
                                    :
                                    25,
                            Username = emailConfigXml.Element("username")?.Value,
                            Password = emailConfigXml.Element("password")?.Value,
                            Domain = emailConfigXml.Element("domain")?.Value,
                            FromEmail = emailConfigXml.Element("fromEmail").Value,
                            SubjectPrefix =  emailConfigXml.Element("subjectPrefix")?.Value ?? "[CLIENT TO AWS WORKER]",
                            OptionalMessage = emailConfigXml.Element("optionalMessage")?.Value
                        },

                    Recipients =
                        recipientsXml?.Elements()
                            .Select(recipientElement =>
                                new NotificationRecipient
                                {
                                    NotificationType = recipientElement.Name.ToString(),
                                    ContentType = recipientElement.Attribute("type").Value,
                                    Address = recipientElement.Value,
                                    DisplayName = 
                                        recipientElement.Attribute("display")?.Value ?? recipientElement.Value
                                })
                            .ToList()
                };

            //return value
            return notificationSettings;
        }
    }
}

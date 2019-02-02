using IPChange.Core.Model;
using IPChange.Core.Model.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace IPChange.Core
{
    /// <summary>
    /// The Notification Worker sends out notifications to configured recipients
    /// </summary>
    public class NotificationWorker : BaseAwsWorker
    {
        public NotificationWorker(
            Config config,
            IpState ipState,
            Action<string> output,
            IEnumerable<(DateTime time, string outputText)> recordedLog, 
            MultiClientState multiClientState) : base(config, ipState, output, multiClientState)
        {
            //Validate
            if (config.NotificationSettings == null)
            {
                throw new ArgumentException("Cannot create a NotificationWorker when Configuration.NotificationSettings are null.");
            }

            bool hasEmailRecipients =
                Config.NotificationSettings.Recipients
                    .Where(nri => nri.NotificationType == "email")
                    .Any();

            if (hasEmailRecipients && config.NotificationSettings.EmailConfig == null)
            {
                throw new ApplicationException("Cannot notify e-mail recipients when there is no e-mail config.");
            }

            //Set internal variables
            this.recordedLog = recordedLog;
        }

        /// <summary>
        /// The record of all the logged messages so far in this process.
        /// </summary>
        protected IEnumerable<(DateTime time, string outputText)> recordedLog;

        /// <summary>
        /// The SMTP Client object used to send e-mail messages with
        /// </summary>
        protected SmtpClient SmtpClient
        {
            get
            {
                if (!_smtpClientSet)
                {
                    if (Config.NotificationSettings.EmailConfig != null)
                    {
                        _smtpClient =
                            new SmtpClient(
                                Config.NotificationSettings.EmailConfig.SmtpServer,
                                Config.NotificationSettings.EmailConfig.SmtpServerPort)
                            {
                                EnableSsl = (Config.NotificationSettings.EmailConfig.SmtpServerPort != 25)
                            };

                        if (!string.IsNullOrWhiteSpace(Config.NotificationSettings.EmailConfig.Username))
                        {
                            _smtpClient.Credentials =
                                new NetworkCredential(
                                    Config.NotificationSettings.EmailConfig.Username,
                                    Config.NotificationSettings.EmailConfig.Password,
                                    Config.NotificationSettings.EmailConfig.Domain);
                        }
                    }

                    _smtpClientSet = true;
                }

                return _smtpClient;
            }
        }
        private SmtpClient _smtpClient = null;
        private bool _smtpClientSet = false;

        /// <summary>
        /// The Host Names (if any) comma separated
        /// </summary>
        protected string HostNames
        {
            get
            {
                if (!_hostNamesSet)
                {
                    if (Config.Route53Hosts != null)
                    {
                        _hostNames =
                            string.Join(
                                ", ",
                                Config.Route53Hosts
                                    .Select(r53hi => r53hi.Name));

                        _hostNamesSet = true;
                    }
                }

                return _hostNames;
            }
        }
        private string _hostNames;
        private bool _hostNamesSet = false;

        /// <summary>
        /// The Summary Message Container
        /// </summary>
        public (string MessageSubject, string MessageBody) SummaryMessage
        {
            get
            {
                if (!_summaryMessageSet)
                {
                    //Set Subject on Message
                    _summaryMessage.MessageSubject = $"IP Address changed for host(s) {HostNames}. {IpState}";

                    if (!string.IsNullOrWhiteSpace(Config.NotificationSettings.EmailConfig.SubjectPrefix))
                    {
                        _summaryMessage.MessageSubject =
                            Config.NotificationSettings.EmailConfig.SubjectPrefix.Trim()
                            + ": "
                            + _fullMessage.MessageSubject;
                    }

                    //Set Body on Message
                    _summaryMessage.MessageBody =
                        "*** IP CHANGE NOTIFICATION *** \n" +
                        "Host Name(s):".PadRight(25) + "\t" + HostNames + "\n" +
                        "OLD IP Address:".PadRight(25) + "\t" + IpState.OldIP + "\n" +
                        "NEW IP Address:".PadRight(25) + "\t" + IpState.NewIP + "\n" +
                        "\n";

                    //Optional Message
                    if (!string.IsNullOrWhiteSpace(Config.NotificationSettings.EmailConfig.OptionalMessage))
                    {
                        _summaryMessage
                            .MessageBody +=
                                "\nMESSAGE:\n" +
                                Config.NotificationSettings.EmailConfig.OptionalMessage +
                                "\n";
                    }

                    //And the Message has been set
                    _summaryMessageSet = true;
                }

                return _summaryMessage;
            }
        }
        private (string MessageSubject, string MessageBody) _summaryMessage;
        private bool _summaryMessageSet = false;

        /// <summary>
        /// The Full Message Container
        /// </summary>
        public (string MessageSubject, string MessageBody) FullMessage
        {
            get
            {
                if (!_fullMessageSet)
                {
                    //Set Subject on Message
                    _fullMessage.MessageSubject = $"IP Address changed for host(s) {HostNames}. {IpState}";

                    if (!string.IsNullOrWhiteSpace(Config.NotificationSettings.EmailConfig.SubjectPrefix))
                    {
                        _fullMessage.MessageSubject =
                            Config.NotificationSettings.EmailConfig.SubjectPrefix.Trim()
                            + ": "
                            + _fullMessage.MessageSubject;
                    }

                    //Set Body on Message
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine(SummaryMessage.MessageBody);
                    sb.AppendLine("");

                    if (recordedLog?.Any() ?? false)
                    {
                        sb.AppendLine("LOG:");

                        recordedLog
                            .Select(li => li.outputText)
                            .ToList()
                            .ForEach(si => sb.AppendLine("\t" + si));
                    }

                    _fullMessage.MessageBody = sb.ToString();

                    //And the Message has been set
                    _fullMessageSet = true;
                }

                return _fullMessage;
            }
        }
        private (string MessageSubject, string MessageBody) _fullMessage;
        private bool _fullMessageSet = false;

        /// <summary>
        /// Will run all the items passed in with the config
        /// </summary>
        /// <returns>
        /// The number of items processed
        /// </returns>
        public int RunAll() => RunItems(Config.NotificationSettings.Recipients);

        /// <summary>
        /// Will run specific items
        /// </summary>
        /// <param name="items">The items to run</param>
        /// <returns>
        /// The number of items processed
        /// </returns>
        public int RunItems(IEnumerable<NotificationRecipient> items)
        {
            //Sort recipients: Take summary recipients first
            items =
                items
                    .OrderBy(
                        nri =>
                            {
                                switch (nri.NotificationType?.ToLowerInvariant())
                                {
                                    case "full":
                                        return 2;

                                    case "summary":
                                        return 0;

                                    default:
                                        return 1;
                                }
                            })
                    .ToList();

            //Run Items
            int completedItems = 0;

            foreach (NotificationRecipient item in items)
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
        public void RunItem(NotificationRecipient item)
        {
            switch (item.NotificationType?.ToLowerInvariant())
            {
                case "email":
                    RunEmailItem(item);
                    break;

                default:
                    throw new NotSupportedException($"The Notification Type \"{item.NotificationType}\" is not supported.");

            }
        }

        /// <summary>
        /// Run a single e-mail item
        /// </summary>
        /// <param name="item">The e-mail item to run</param>
        public void RunEmailItem(NotificationRecipient item)
        {
            //Validate
            if (!string.Equals(item.NotificationType, "email", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ApplicationException($"{nameof(RunEmailItem)} can only process items with Notification Type \"email\". Was \"{item.NotificationType}\"");
            }

            //Set message variable
            (string MessageSubject, string MessageBody) message;

            //Get the body text of the message depending 
            switch (item.ContentType?.ToLowerInvariant())
            {
                case "full":
                    message = FullMessage;
                    break;

                case "summary":
                    message = SummaryMessage;
                    break;

                default:
                    throw new ApplicationException($"The message Content Type \"{item.ContentType}\" is not supported.");

            }

            //Create that Mail Message
            MailMessage mailMessage =
                new MailMessage(Config.NotificationSettings.EmailConfig.FromEmail, item.Address)
                {
                    Subject = message.MessageSubject,
                    Body = message.MessageBody
                };

            //Send email
            SmtpClient.Send(mailMessage);

            //Output
            Output($"NOTIFICATION: Sent {item.NotificationType} {item.ContentType} to {item.Address}.");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _smtpClient?.Dispose();
            }
        }

        public override string ToString() => $"AWS Notification Worker: {IpState}";
    }
}

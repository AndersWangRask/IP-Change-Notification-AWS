using System;
using System.Collections.Generic;
using System.Text;

namespace IPChange.Core.Model.Config
{
    /// <summary>
    /// Configuration on a single recipient to Notify.
    /// Currently only email is supported.
    /// </summary>
    public class NotificationRecipient
    {
        /// <summary>
        /// What type of notification.
        /// Currently only email is supported.
        /// </summary>
        public string NotificationType { get; set; }

        /// <summary>
        /// What type of message content to send.
        /// Currently supported "full" and "summary".
        /// Note that "full" *may* include confidential information.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Display name of the recipient
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Address of the recipient.
        /// E.g. an e-mail address (jlp@enterprise-d.com)
        /// </summary>
        public string Address { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IPChange.Core.Model.Config
{
    /// <summary>
    /// Config Settings for sending notifications when the IP changes
    /// </summary>
    public class NotificationSettings
    {
        /// <summary>
        /// Settings for sending e-mails
        /// </summary>
        public NotificationsEmailConfig EmailConfig { get; set; }

        /// <summary>
        /// List of recipients to send notifications to.
        /// </summary>
        public IEnumerable<NotificationRecipient> Recipients { get; set; }
    }
}
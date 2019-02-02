using System;
using System.Collections.Generic;
using System.Text;

namespace IPChange.Core.Model.Config
{
    /// <summary>
    /// Settings for sending e-mail
    /// </summary>
    public class NotificationsEmailConfig
    {
        /// <summary>
        /// The full name of the SMTP server to use.
        /// E.g. smtp.gmail.com.
        /// </summary>
        public string SmtpServer { get; set; }

        /// <summary>
        /// The port to connect to on the server.
        /// Typically 25 for insecure SMTP or 486 or 587 for secure (encrypted connection) SMTP.
        /// </summary>
        /// <remarks>
        /// Using Port 25 will automatically set the connection to insecure (i.e. not encrypted connection)
        /// Using any other Port will automatically set the connection to secure (i.e. will encrypt connection)
        /// </remarks>
        public int SmtpServerPort { get; set; }

        /// <summary>
        /// Username to authenticate with.
        /// If omitted any password and domain will be ignored.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Password to authenticate with.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Domain (if any) to authenticate with.
        /// </summary>
        /// <remarks>
        /// Please note that this is a domain for the username account.
        /// Practically this is almost only the case for Windows AD account.
        /// This is NOT related to the DNS name of the e-mail address or the SMTP server.
        /// If in doubt, leave it empty.
        /// </remarks>
        public string Domain { get; set; }

        /// <summary>
        /// Who the sender of the e-mail notification will be set to.
        /// </summary>
        /// <remarks>
        /// This can be set to just the e-mail address (e.g. "jlp@enterprise-d.com")
        /// or with a display name. (e.g. "Jean-Luc Picard &lt;jlp@enterprise-d.com&gt;")
        /// </remarks>
        public string FromEmail { get; set; }

        /// <summary>
        /// (Optional) If set will be prefixed to the subject of the e-mail
        /// </summary>
        public string SubjectPrefix { get; set; }

        /// <summary>
        /// (Optional) If set will be inserted in the beginning of the e-mail body message
        /// </summary>
        public string OptionalMessage { get; set; }
    }
}

using System.Collections.Generic;
using System.Net.Mail;
using MailDaemon.Lib;

namespace MailDaemon.Core
{
    public interface IMailDaemonService
    {
        string MailProfileFilename { get; set; }

        /// <summary>
        /// If true - no any mail send to recipients.
        /// </summary>
        bool JustValidate { get; set; }

        /// <summary>
        /// If true - send demo mail to sender. No any mail send to recipients.
        /// </summary>
        bool SendDemo { get; set; }

        /// <summary>
        /// Delay sending mail to avoid stressful SMTP server.
        /// </summary>
        int SendSleep { get; set; }

        List<MessageInfo> Errors { get; set; }

        List<string> Warnings { get; set; }

        MailProfile MailProfile { get; set; }

        void ReadMailProfile();
        void ValidateMailProfile();
        string ReadMailBodyTemplate(string filePath);      
        MailAddress GetMailAddress(string address, string name = "");
        MailMessage GenerateMailMessage(RecipientInfo recipientInfo);
        string FormatMessageSubject(RecipientInfo recipientInfo);
        string FormatMessageBody(RecipientInfo recipientInfo);
    }
}

using System.Net.Mail;

namespace BlackNight.MailDaemon
{
    public interface IMailDaemon
    {
        void ReadMailProfile();
        void ValidateMailProfile();
        string ReadMailBodyTemplate(string filePath);      
        MailAddress GetMailAddress(string address, string name = "");
        MailMessage GenerateMailMessage(RecipientInfo recipientInfo);
        string FormatMessageSubject(RecipientInfo recipientInfo);
        string FormatMessageBody(RecipientInfo recipientInfo);
    }
}

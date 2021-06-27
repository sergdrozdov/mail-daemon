using System.Net.Mail;

namespace BlackNight.MailDaemon
{
    public interface IMailAgent
    {
        MailSendResult Send(MailMessage mailMessage);
    }
}

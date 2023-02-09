using System.Net.Mail;

namespace BlackNight.MailDaemon.Core
{
    public interface IMailAgent
    {
        MailSendResult Send(MailMessage mailMessage);
    }
}

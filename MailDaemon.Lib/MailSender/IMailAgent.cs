using System.Net.Mail;

namespace MailDaemon.Core
{
    public interface IMailAgent
    {
        MailSendResult Send(MailMessage mailMessage);
    }
}

using System;
using System.Net;
using System.Net.Mail;

namespace BlackNight.MailDaemon
{
	public class MailAgent : IMailAgent
    {
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public bool SmtpEnableSSL { get; set; }

        public MailSendResult Send(MailMessage mailMessage)
		{
            try
            {
                var smtpClient = new SmtpClient();
                smtpClient.Host = SmtpHost;
                smtpClient.Port = SmtpPort;
                smtpClient.EnableSsl = SmtpEnableSSL;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(SmtpUsername, SmtpPassword);
                smtpClient.Send(mailMessage);

                return new MailSendResult { Success = true, Message = "Mail sent to recipient(s)." };
            }
			catch (SmtpException ex)
            {
                return new MailSendResult { Success = false, Message = ex.Message };
            }
        }
    }
}
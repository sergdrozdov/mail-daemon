using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Text;

namespace MailDaemon.Core
{
    public class MailDaemonService : IMailDaemonService
    {
        public string MailProfileFilename { get; set; }
        public bool JustValidate { get; set; }
        public bool SendDemo { get; set; }
        public RecipientInfo DemoRecipient { get; set; }
        public int SendSleep { get; set; } = 1000;
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public MailProfile MailProfile { get; set; }

        public MailDaemonService()
		{
			MailProfile = new MailProfile();
            DemoRecipient = new RecipientInfo();
            Errors = new List<string>();
			Warnings = new List<string>();
		}

        public void ReadMailProfile()
		{
            try
            {
                MailProfile = JsonConvert.DeserializeObject<MailProfile>(File.ReadAllText(MailProfileFilename));
            }
            catch (Exception ex)
            {
               throw new Exception(ex.Message);
            }
        }

		public string ReadMailBodyTemplate(string filePath)
		{
			try
			{
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    using (var sr = new StreamReader(filePath))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}

			return "";
		}

		public void ValidateMailProfile()
		{
			// validate sender
			if (MailProfile.Sender == null)
			{
				Errors.Add($"Mail 'sender' property in '{MailProfileFilename}' not exists.");
			}
			else if (string.IsNullOrEmpty(MailProfile.Sender.Address))
			{
				Errors.Add($"Mail 'sender' address value in '{MailProfileFilename}' is empty.");
			}
			else if (!MailProfile.Sender.Address.ValidateEmail())
			{
				Errors.Add($"Mail 'sender' address '{MailProfile.Sender.Address}' not valid.");
			}

			if (string.IsNullOrEmpty(MailProfile.Subject))
			{
				Errors.Add($"Mail 'subject' value in '{MailProfileFilename}' is empty.");
			}

			// validate recipients
			if (MailProfile.Recipients == null)
			{
				Errors.Add($"Mail 'recipients' property in '{MailProfileFilename}' not exists.");
			}
			else if (MailProfile.Recipients.Count == 0)
			{
				Errors.Add("No mail recipients found.");
			}
			else
			{
				foreach (var recipient in MailProfile.Recipients)
				{
					if (!recipient.Address.ValidateEmail())
					{
						Errors.Add($"Mail 'recipient' address '{recipient.Address}' not valid.");
					}

					if (recipient.Attachments != null)
					{
						foreach (var attachment in recipient.Attachments)
						{
							if (string.IsNullOrEmpty(attachment.Path))
							{
								Warnings.Add($"Attachment file path for recipient '{recipient.Address}' is empty.");
								continue;
							}

							if (!File.Exists(attachment.Path))
							{
								Warnings.Add($"Attachment '{attachment.Path}' for recipient '{recipient.Address}' not found.");
							}
						}
					}
				}
			}

			// validate mail template
			if (string.IsNullOrEmpty(MailProfile.MailBodyTemplate))
			{
				Errors.Add($"Mail 'template' property in '{MailProfileFilename}' is empty.");
			}
			else
			{
				if (!File.Exists(MailProfile.MailBodyTemplate))
				{
					Errors.Add($"Mail body template file '{MailProfile.MailBodyTemplate}' not found.");
				}
			}

			// validate attachments
			if (MailProfile.Attachments != null)
			{
				foreach (var attachment in MailProfile.Attachments)
				{
					if (string.IsNullOrEmpty(attachment.Path))
					{
						Warnings.Add($"Attachment file path for mail profile '{MailProfileFilename}' is empty.");
						continue;
					}

					if (!File.Exists(attachment.Path))
					{
						Errors.Add($"Attachment '{attachment.Path}' for mail profile '{MailProfileFilename}' not found.");
					}
				}
			}
		}

		public MailAddress GetMailAddress(string address, string name = "")
		{
			if (string.IsNullOrEmpty(address) && string.IsNullOrEmpty(name))
				throw new ArgumentException("Address and name both empty.");
			if (string.IsNullOrEmpty(address))
				throw new ArgumentException("Address is empty.");

			return !string.IsNullOrEmpty(name) ? new MailAddress(address, name)	: new MailAddress(address);
		}

		public MailMessage GenerateMailMessage(RecipientInfo recipientInfo)
		{
			var mailMessage = new MailMessage();		
			
			if (SendDemo)
			{
                // send as demo to sender
                mailMessage.To.Add(GetMailAddress(DemoRecipient.Address, DemoRecipient.Name));
                mailMessage.From = GetMailAddress(MailProfile.Sender.Address, MailProfile.Sender.Name);
                mailMessage.ReplyToList.Add(mailMessage.From);
                mailMessage.Headers.Add("Reply-To", MailProfile.Sender.Address);
			}
			else
			{
                // send to recipient
                mailMessage.To.Add(GetMailAddress(recipientInfo.Address, recipientInfo.Name));
                mailMessage.From = GetMailAddress(MailProfile.Sender.Address, MailProfile.Sender.Name);
                mailMessage.ReplyToList.Add(mailMessage.From);
                mailMessage.Headers.Add("Reply-To", MailProfile.Sender.Address);
			}

            mailMessage.SubjectEncoding = Encoding.UTF8;
			mailMessage.Subject = FormatMessageSubject(recipientInfo);
			if (SendDemo)
				mailMessage.Subject += " [DEMO MAIL]";

			mailMessage.IsBodyHtml = true;
			mailMessage.BodyEncoding = Encoding.UTF8;
			mailMessage.Body = FormatMessageBody(recipientInfo);

			// mail profile attachments
			if (MailProfile.Attachments != null)
			{
				foreach (var attachment in MailProfile.Attachments)
				{
					if (!File.Exists(attachment.Path))
						continue;
					var fileStream = new StreamReader(attachment.Path);
					if (!string.IsNullOrEmpty(attachment.FileName))
						mailMessage.Attachments.Add(new Attachment(fileStream.BaseStream, attachment.FileName, Helpers.GetMediaType(attachment.Path)));
					else
						mailMessage.Attachments.Add(new Attachment(fileStream.BaseStream, Path.GetFileName(attachment.Path), Helpers.GetMediaType(attachment.Path)));
				}
			}

			// recipient related attachments
			if (recipientInfo.Attachments != null)
			{
				foreach (var attachment in recipientInfo.Attachments)
				{
					if (File.Exists(attachment.Path))
					{
						var fileStream = new StreamReader(attachment.Path);
						if (!string.IsNullOrEmpty(attachment.FileName))
							mailMessage.Attachments.Add(new Attachment(fileStream.BaseStream, attachment.FileName, Helpers.GetMediaType(attachment.Path)));
						else
							mailMessage.Attachments.Add(new Attachment(fileStream.BaseStream, Path.GetFileName(attachment.Path), Helpers.GetMediaType(attachment.Path)));
					}
				}
			}

			return mailMessage;
		}

		public string FormatMessageSubject(RecipientInfo recipientInfo)
		{
			var subject = !string.IsNullOrEmpty(recipientInfo.Subject) ? recipientInfo.Subject : MailProfile.Subject;
			subject = subject
				.Replace("{PERSON_NAME}", recipientInfo.Name)
				.Replace("{COMPANY_NAME}", recipientInfo.Company);
			
			return subject;
		}

		public string FormatMessageBody(RecipientInfo recipientInfo)
		{
			var body = !string.IsNullOrEmpty(recipientInfo.MailBody) ? recipientInfo.MailBody : MailProfile.MailBody;
			body = body
				.Replace("{PERSON_NAME}", recipientInfo.Name)
				.Replace("{COMPANY_NAME}", recipientInfo.Company)
                .Replace("{CONTACT_PERSON}", recipientInfo.ContactPerson);

            return body;
		}
	}
}

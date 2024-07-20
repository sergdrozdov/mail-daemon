using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;
using MailDaemon.Lib;
using MailDaemon.Core;

namespace MailDaemon.Core
{
    public class MailDaemonService : IMailDaemonService
    {
        public string MailProfileFilename { get; set; }
        public bool JustValidate { get; set; }
        public bool SendDemo { get; set; }

        /// <summary>
        /// Create processed mails on disk.
        /// </summary>
        public bool GeneratePreview { get; set; }

        /// <summary>
        /// The person who sends mails.
        /// </summary>
        public RecipientInfo Operator { get; set; }
        public int SendSleep { get; set; } = 1000;
        public List<MessageInfo> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public MailProfile MailProfile { get; set; }

        public MailDaemonService()
		{
			MailProfile = new MailProfile();
            Operator = new RecipientInfo();
            Errors = new List<MessageInfo>();
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
				Errors.Add(new MessageInfo
                    {
                        Message = $"Mail \"sender\" property in \"{MailProfileFilename}\" not exists.",
					    IsCritical = true
                    }
                );
			}
			else if (string.IsNullOrEmpty(MailProfile.Sender.Address))
			{
				Errors.Add(new MessageInfo
                    {
						Message = $"Mail \"sender\" address value in \"{MailProfileFilename}\" is empty.",
                        IsCritical = true
                    }
                );
			}
			else if (!MailProfile.Sender.Address.ValidateEmail())
			{
				Errors.Add(new MessageInfo
                    {
                        Message = $"Mail \"sender\" address \"{MailProfile.Sender.Address}\" not valid.",
                        IsCritical = true
                    }
                );
			}

			if (string.IsNullOrEmpty(MailProfile.Subject))
			{
				Errors.Add(new MessageInfo
                    {
                        Message = $"Mail \"subject\" value in \"{MailProfileFilename}\" is empty.",
                        IsCritical = true
                    }
                );
			}

			// validate recipients
			if (MailProfile.Recipients == null)
			{
				Errors.Add(new MessageInfo
                    {
                        Message = $"Mail \"recipients\" property in \"{MailProfileFilename}\" not exists.",
                        IsCritical = true
                    }
                );
			}
			else if (MailProfile.Recipients.Count == 0)
			{
				Errors.Add(new MessageInfo
                    {
                        Message = "No mail recipients found.",
                        IsCritical = true
                    }
                );
			}
			else
			{
				foreach (var recipient in MailProfile.Recipients)
				{
					if (!recipient.Address.ValidateEmail())
					{
						Errors.Add(new MessageInfo
                            {
                                Message = $"Mail \"recipient\" address \"{recipient.Address}\" not valid.",
                                IsCritical = true
                            }
                        );
					}

					if (recipient.Attachments != null)
					{
						foreach (var attachment in recipient.Attachments)
						{
							if (string.IsNullOrEmpty(attachment.Path))
							{
								Warnings.Add($"Attachment file path for recipient \"{recipient.Address}\" is empty.");
								continue;
							}

							if (!File.Exists(attachment.Path))
							{
								Warnings.Add($"Attachment \"{attachment.Path}\" for recipient \"{recipient.Address}\" not found.");
							}
						}
					}
				}
			}

			// validate mail template
			if (string.IsNullOrEmpty(MailProfile.MailBodyTemplateFilePath))
			{
				Errors.Add(new MessageInfo
                    {
                        Message = $"Mail \"template\" property in \"{MailProfileFilename}\" is empty.",
                        IsCritical = true
                    }
                );
			}
			else
			{
				if (!File.Exists(MailProfile.MailBodyTemplateFilePath))
				{
					Errors.Add(new MessageInfo
                        {
                            Message = $"Mail body template file \"{MailProfile.MailBodyTemplateFilePath}\" not exists.",
                            IsCritical = true
                        }
                    );
				}
			}

			// validate attachments
			if (MailProfile.Attachments != null)
			{
				foreach (var attachment in MailProfile.Attachments)
				{
					if (string.IsNullOrEmpty(attachment.Path))
					{
						Warnings.Add($"Attachment file path for mail profile \"{MailProfileFilename}\" is empty.");
						continue;
					}

					if (!File.Exists(attachment.Path))
					{
						Errors.Add(new MessageInfo
                            {
                                Message = $"Attachment \"{attachment.Path}\" for mail profile \"{MailProfileFilename}\" not exists.",
                                IsCritical = true
                            }
                        );
					}
				}
			}
		}

        public void AddError(string errorMessage, bool isCritical = false)
        {
			Errors.Add(new MessageInfo()
            {
				IsCritical = isCritical,
				Message = errorMessage
            });
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
                mailMessage.To.Add(GetMailAddress(Operator.Address, Operator.Name));
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

			if (Path.GetExtension(recipientInfo.MailBodyTemplateFilePath).ToLower() != ".txt")
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

            // recipient's replacement dictionary has higher priority
            if (recipientInfo.Replace != null)
            {
                foreach (var replaceData in recipientInfo.Replace)
                {
                    body = body.Replace("{" + replaceData.Key + "}", replaceData.Value, StringComparison.InvariantCultureIgnoreCase);
                }
            }

            if (MailProfile.Replace != null)
            {
                foreach (var replaceData in MailProfile.Replace)
                {
                    body = body.Replace("{" + replaceData.Key + "}", replaceData.Value, StringComparison.InvariantCultureIgnoreCase);
                }
            }

            return body;
		}
	}
}

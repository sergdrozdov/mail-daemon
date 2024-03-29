﻿using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading;
using BlackNight.MailDaemon.Core;

namespace BlackNight.MailDaemon.ConsoleApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables().Build();
			
			var displayHelp = false;
			var mailDaemon = new Core.MailDaemon();
			var mailAgent = new MailAgent();

            if (args.Length > 0)
			{
				try
				{
					for (var i = 0; i < args.Length; i++)
					{
						switch (args[i].ToLower())
						{
							case "-v":
								mailDaemon.JustValidate = true;
								break;
							case "-d":
								mailDaemon.SendDemo = true;
								break;
							case "-h":
								displayHelp = true;
								break;
						}
					}
				}
				catch (Exception ex)
				{
					DisplayErrorMessage(ex.Message);
					return;
				}
			}

			Console.WriteLine("=== BlackNight Mail Daemon 0.7 ===");
			Console.WriteLine("Author:\t\tSergey Drozdov");
			Console.WriteLine("Email:\t\tsergey.drozdov.0305@gmail.com");
			Console.WriteLine("Website:\thttps://sd.blackball.lv/sergey-drozdov");
			Console.Write(Environment.NewLine);

			if (displayHelp)
			{
				Console.WriteLine("Description:");
				Console.WriteLine("-v\t\tValidation mode to verify mail profile integrity. With this argument mails not sending to recipients.");
				Console.WriteLine("-d\t\tSend demo mail only to sender. With this argument mails not sending to recipients.");
				WaitForExit();
				return;
			}

			// configure SMTP server info
			mailAgent.SmtpHost = configuration["MailServer:SmtpHost"];
			mailAgent.SmtpPort = Convert.ToInt32(configuration["MailServer:SmtpPort"]);
			mailAgent.SmtpUsername = configuration["MailServer:SmtpUsername"];
			mailAgent.SmtpPassword = configuration["MailServer:SmtpPassword"];
			mailAgent.SmtpEnableSSL = Convert.ToBoolean(configuration["MailServer:SmtpEnableSSL"]);

			if (mailDaemon.JustValidate)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("--- Validation mode: do not send any mail. Just validate mail profile and recipients.");
				Console.WriteLine("");
				Console.ResetColor();
			}

			mailDaemon.MailProfileFilename = Path.Combine(Environment.CurrentDirectory, "MailProfiles", configuration["App:MailProfile"]);
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"--- Mail profile: '{mailDaemon.MailProfileFilename}'");
			Console.ResetColor();
			
			mailDaemon.ReadMailProfile();
			mailDaemon.ValidateMailProfile();

			if (mailDaemon.MailProfile.MailBodyTemplate.StartsWith(".\\"))
				mailDaemon.MailProfile.MailBodyTemplate = Path.Combine(Environment.CurrentDirectory, mailDaemon.MailProfile.MailBodyTemplate.Replace(".\\", ""));
			mailDaemon.MailProfile.MailBody = mailDaemon.ReadMailBodyTemplate(mailDaemon.MailProfile.MailBodyTemplate);

			// show errors
			if (mailDaemon.Errors.Count > 0)
			{
				SetErrorMessagesStyle();
				Console.WriteLine("");
				Console.WriteLine("Errors:");
				foreach (var message in mailDaemon.Errors)
				{
					DisplayErrorMessage(message);
				}
			}

			// show warnings
			if (mailDaemon.Warnings.Count > 0)
			{
				SetWarningMessagesStyle();
				Console.WriteLine("");
				Console.WriteLine("Warnings:");
				foreach (var message in mailDaemon.Warnings)
				{
					DisplayWarningMessage(message);
				}
			}

			// if mail profile contains errors - stop execution
			if (mailDaemon.Errors.Count > 0)
			{
				WaitForExit();
				return;
			}

			// if mail profile contains warnings - ask user to continue or not
			if (mailDaemon.Warnings.Count > 0)
			{
				Console.WriteLine("");
				Console.Write("Continue? [Y/N]");

				var confirmed = false;
				string key;
				while (!confirmed)
				{
					key = Console.ReadLine().ToLower();

					if (key == "y" || key == "n")
					{
						confirmed = true;
					}
				}
			}			

			var counter = 0;
			var recipientsReport = new StringBuilder();
			foreach (var recipient in mailDaemon.MailProfile.Recipients)
			{
				var recipientReportInfo = new StringBuilder();
				try
				{
					var mailMessage = mailDaemon.GenerateMailMessage(recipient);

					// display mail sending process
                    counter++;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"({counter}) {recipient.Company?.ToUpper()} {recipient.Name}");
                    Console.WriteLine($"Mail: {recipient.Address}");
					Console.WriteLine($"Subject: {mailMessage.Subject}");
					Console.WriteLine($"Template: {(!string.IsNullOrEmpty(recipient.MailBodyTemplate) ? recipient.MailBodyTemplate : mailDaemon.MailProfile.MailBodyTemplate)}");

					if (!string.IsNullOrEmpty(recipient.MailBodyTemplate))
                        mailDaemon.MailProfile.MailBody = mailDaemon.ReadMailBodyTemplate(recipient.MailBodyTemplate);
					else
                        mailDaemon.MailProfile.MailBody = mailDaemon.ReadMailBodyTemplate(mailDaemon.MailProfile.MailBodyTemplate);

                    if (recipient.Skip.GetValueOrDefault())
                        recipientReportInfo.AppendLine("<div style=\"color: #999\">");
                    recipientReportInfo.AppendLine($"({counter}) {recipient.Company?.ToUpper()} {recipient.Name} <a href=\"mailto:{recipient.Address}\">{recipient.Address}</a>");
					recipientReportInfo.AppendLine($"<div>Subject: {mailMessage.Subject}</div>");

					// attachments
					if (mailDaemon.MailProfile.Attachments != null)
					{
						foreach (var attachment in mailDaemon.MailProfile.Attachments)
						{
							if (File.Exists(attachment.Path))
							{
								Console.WriteLine($"\tAttachment: '{attachment.Path}'");
								recipientReportInfo.AppendLine($"<div style=\"padding-left: 40px\">Attachment: '{attachment.Path}'</div>");
							}
							else
							{
								DisplayWarningMessage($"\tAttachment: file '{attachment.Path}' not exists.");
								recipientReportInfo.AppendLine($"<div style=\"padding-left: 40px\">Attachment: file '{attachment.Path}' not exists.</div>");
							}
						}
					}

					// recipient related attachments
					if (recipient.Attachments != null)
					{
						foreach (var attachment in recipient.Attachments)
						{
							if (File.Exists(attachment.Path))
							{
								Console.WriteLine($"\tAttachment: '{attachment.Path}'");
								recipientReportInfo.AppendLine($"<div style=\"padding-left: 40px\">Attachment: '{attachment.Path}'</div>");
							}
							else
							{
								DisplayWarningMessage($"\tAttachment: file \"{attachment.Path}\" not exists.");
								recipientReportInfo.AppendLine($"<div style=\"padding-left: 40px; color: #aa0000\">Attachment: file '{attachment.Path}' not exists.</div>");
							}
						}
					}

                    if (recipient.Skip.GetValueOrDefault())
                    {
                        recipientReportInfo.AppendLine("--- Skipped ---");
                        recipientReportInfo.AppendLine("</div>");
                    }

                    if (mailDaemon.SendDemo)
					{
						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.WriteLine($"--- Send demo to sender address: {mailDaemon.MailProfile.Sender.Address} ---");
						Console.ResetColor();
					}

					if (!mailDaemon.JustValidate)
					{
                        if (recipient.Skip.GetValueOrDefault())
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("--- Skipped ---");
                            Console.ResetColor();
                            Console.WriteLine("");
                        }
                        else
                        {
                            var mailSendResult = mailAgent.Send(mailMessage);

						    if (!mailSendResult.Success)
						    {
							    DisplayErrorMessage(mailSendResult.Message);
						    }
						    else
						    {
							    Console.ForegroundColor = ConsoleColor.Green;
							    Console.WriteLine("--- Sent ---");
							    Console.ResetColor();
							    Console.WriteLine("");
						    }
                        }
					}
                    else
                    {
                        if (recipient.Skip.GetValueOrDefault())
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("--- Skipped ---");
                            Console.ResetColor();
                        }
						Console.WriteLine("");
                    }
                }
				catch (Exception ex)
				{
					DisplayErrorMessage(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
					Console.WriteLine("--- Error ---");
					Console.WriteLine("");
				}

				recipientReportInfo.AppendLine("<br/>");
				recipientsReport.AppendLine(recipientReportInfo.ToString());
				Thread.Sleep(mailDaemon.SendSleep);
			}

			if (!mailDaemon.JustValidate)
			{
				// send status report to sender
				try
				{
					var mailMessage = new MailMessage();
					mailMessage.To.Add(mailDaemon.GetMailAddress(mailDaemon.MailProfile.Sender.Address, mailDaemon.MailProfile.Sender.Name));
					mailMessage.From = mailDaemon.GetMailAddress(mailDaemon.MailProfile.Sender.Address, mailDaemon.MailProfile.Sender.Name);
					mailMessage.ReplyToList.Add(mailMessage.From);
					mailMessage.Headers.Add("Reply-To", mailDaemon.MailProfile.Sender.Address);
					mailMessage.Subject = "Mail Daemon: mails has been sent";
					mailMessage.SubjectEncoding = Encoding.UTF8;
					mailMessage.IsBodyHtml = true;
					mailMessage.BodyEncoding = Encoding.UTF8;

					var report = new StringBuilder();
					report.AppendLine("<!DOCTYPE html>");
					report.AppendLine("<html>");
					report.AppendLine("<head>");
					report.AppendLine("<meta charset=\"utf-8\" />");
					report.AppendLine("<title>Mail Daemon report</title>");
					report.AppendLine("</head>");
					report.AppendLine("<body>");
					report.AppendLine($"<div>{mailDaemon.MailProfile.Recipients.Count} mails has been sent.</div>");
					report.AppendLine($"<div>Mail profile: '{mailDaemon.MailProfileFilename}'</div>");
					report.AppendLine($"<div>Mail template: '{mailDaemon.MailProfile.MailBodyTemplate}'</div>");
					report.AppendLine("<br/>");
					report.AppendLine($"<div><strong>Recipients:</strong></div>");
					report.AppendLine($"<div>{recipientsReport}</div>");
					report.AppendLine("</body>");
					report.AppendLine("</html>");
					mailMessage.Body = report.ToString();

					mailAgent.Send(mailMessage);

					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine("--- Mails has been sent ---");
					Console.ForegroundColor = ConsoleColor.White;
				}
				catch (Exception ex)
				{
					DisplayErrorMessage(ex.InnerException != null ? ex.InnerException.Message : ex.Message);
					Console.WriteLine("--- Error ---");
				}
				Thread.Sleep(5000);
			}
			else
			{
				WaitForExit();
			}
		}

		private static void SetErrorMessagesStyle()
		{
			Console.ForegroundColor = ConsoleColor.Red;
		}

		private static void SetWarningMessagesStyle()
		{
			Console.ForegroundColor = ConsoleColor.Magenta;
		}

		private static void DisplayErrorMessage(string message)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(message);
			Console.ResetColor();
		}

		private static void DisplayWarningMessage(string message)
		{
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine(message);
			Console.ResetColor();
		}

		private static void WaitForExit()
		{
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine("");
			Console.Write("Press any key to exit...");
			Console.ReadKey();
		}
	}
}

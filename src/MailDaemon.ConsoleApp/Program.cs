using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading;
using MailDaemon.Core;

namespace MailDaemon.ConsoleApp
{
    internal class Program
    {
		private static string PreviewsDirPath { get; set; }
		private static string ReportsDirPath { get; set; }

        private static void Main(string[] args)
        {
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables().Build();
			
			var displayHelp = false;
			var mailDaemonService = new MailDaemonService();
			var mailAgent = new MailAgent();

            if (args.Length > 0)
			{
				try
                {
                    var argIndex = 0;
                    foreach (var arg in args)
                    {
                        switch (arg.ToLower())
                        {
                            case "-v":
                                mailDaemonService.JustValidate = true;
                                break;
                            case "-d":
                                mailDaemonService.SendDemo = true;
                                break;
                            case "-gp":
                                mailDaemonService.GeneratePreview = true;
                                break;
                            case "-p":
                                mailDaemonService.MailProfileFilename = Path.Combine(Environment.CurrentDirectory, "MailProfiles", args[argIndex + 1]);
                                break;
                            case "-h":
                                displayHelp = true;
                                break;
                        }

                        argIndex++;
                    }
                }
				catch (Exception ex)
				{
					DisplayErrorMessage(ex.Message);
					return;
				}
			}

            if (!string.IsNullOrEmpty(mailDaemonService.MailProfileFilename) && !File.Exists(mailDaemonService.MailProfileFilename))
            {
                DisplayErrorMessage($"Mail profile \"{mailDaemonService.MailProfileFilename}\" not exists.");
                return;
            }

            // TBD: Think about whether someone needs this information
            //Console.WriteLine("=== Mail Daemon 0.8 ===");
            //Console.WriteLine("Author:\t\tSergey Drozdov");
            //Console.WriteLine("Email:\t\tsergey.drozdov.0305@gmail.com");
            //Console.WriteLine("Website:\thttps://sd.blackball.lv/sergey-drozdov");
            //Console.Write(Environment.NewLine);

			if (displayHelp)
			{
				Console.WriteLine("Description:");
				Console.WriteLine("-v\t\tValidation mode to verify mail profile integrity. With this argument mails not sending to recipients.");
				Console.WriteLine("-d\t\tSend demo mail only to sender. With this argument mails not sending to recipients.");
				Console.WriteLine("-gp\t\tCreate files on disk with generated mails for each recipient.");
				Console.WriteLine("-p\t\tName of the mail profile.");
				WaitForExit();
				return;
			}

            // configure SMTP server info
			mailAgent.SmtpHost = configuration["MailServer:SmtpHost"];
			mailAgent.SmtpPort = Convert.ToInt32(configuration["MailServer:SmtpPort"]);
			mailAgent.SmtpUsername = configuration["MailServer:SmtpUsername"];
			mailAgent.SmtpPassword = configuration["MailServer:SmtpPassword"];
			mailAgent.SmtpEnableSSL = Convert.ToBoolean(configuration["MailServer:SmtpEnableSSL"]);

            if (mailDaemonService.JustValidate)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("--- Validation mode: do not send any mail. Just validate mail profile and recipients.");
				Console.WriteLine("");
				Console.ResetColor();
			}

			if (string.IsNullOrEmpty(mailDaemonService.MailProfileFilename))
			    mailDaemonService.MailProfileFilename = Path.Combine(Environment.CurrentDirectory, "MailProfiles", configuration["App:MailProfile"]);
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"--- Mail profile: \"{Path.GetFileName(mailDaemonService.MailProfileFilename)}\"");
			Console.ResetColor();

            try
            {
                mailDaemonService.ReadMailProfile();
            }
            catch (Exception ex)
            {
                mailDaemonService.AddError(ex.Message);
            }

            mailDaemonService.ValidateMailProfile();

            ReportsDirPath = Path.Combine(Environment.CurrentDirectory, "reports");
            if (!Directory.Exists(ReportsDirPath))
                Directory.CreateDirectory(ReportsDirPath);

            if (mailDaemonService.GeneratePreview)
            {
                try
                {
                    PreviewsDirPath = Path.Combine(Environment.CurrentDirectory, "previews", Path.GetFileName(mailDaemonService.MailProfileFilename));
                    if (!Directory.Exists(PreviewsDirPath))
                        Directory.CreateDirectory(PreviewsDirPath);
                    else
                    {
                        foreach (var filePath in Directory.EnumerateFiles(PreviewsDirPath))
                        {
                            File.Delete(filePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DisplayErrorMessage(ex.Message);
                    WaitForExit();
                    return;
                }
            }

            mailDaemonService.Operator.Address = configuration["Operator:address"];
            mailDaemonService.Operator.Name = configuration["Operator:name"];

            if (mailDaemonService.MailProfile.MailBodyTemplateFilePath.StartsWith(".\\"))
				mailDaemonService.MailProfile.MailBodyTemplateFilePath = Path.Combine(Environment.CurrentDirectory, mailDaemonService.MailProfile.MailBodyTemplateFilePath.Replace(".\\", ""));
			mailDaemonService.MailProfile.MailBody = mailDaemonService.ReadMailBodyTemplate(mailDaemonService.MailProfile.MailBodyTemplateFilePath);

			// show errors
			if (mailDaemonService.Errors.Count > 0)
			{
				SetErrorMessagesStyle();
				Console.WriteLine("");
				Console.WriteLine("Errors:");
				foreach (var message in mailDaemonService.Errors)
				{
					DisplayErrorMessage(message.Message);
				}
			}

			// show warnings
			if (mailDaemonService.Warnings.Count > 0)
			{
				SetWarningMessagesStyle();
				Console.WriteLine("");
				Console.WriteLine("Warnings:");
				foreach (var message in mailDaemonService.Warnings)
				{
					DisplayWarningMessage(message);
				}
			}

			// if mail profile contains errors - stop execution
			if (mailDaemonService.Errors.Count > 0)
			{
				WaitForExit();
				return;
			}

			// if mail profile contains warnings - ask user to continue or not
			if (mailDaemonService.Warnings.Count > 0)
			{
				Console.WriteLine("");
				Console.Write("Continue? [Y/N]");

				var confirmed = false;
				string key;
				while (!confirmed)
				{
					key = Console.ReadLine().ToLower();

					if (key == "y")
					{
						confirmed = true;
					}
					if (key == "n")
					{
                        WaitForExit();
						return;
					}
				}
			}			

			var counter = 0;
			var recipientsReport = new StringBuilder();
			foreach (var recipient in mailDaemonService.MailProfile.Recipients)
			{
				var recipientReportInfo = new StringBuilder();
				try
	        	{
                    // TBD: add support for HTML and plain text files
					if (string.IsNullOrEmpty(recipient.MailBodyTemplateFilePath))
                        recipient.MailBodyTemplateFilePath = mailDaemonService.MailProfile.MailBodyTemplateFilePath;

                    mailDaemonService.MailProfile.MailBody = mailDaemonService.ReadMailBodyTemplate(recipient.MailBodyTemplateFilePath);

                    var mailMessage = mailDaemonService.GenerateMailMessage(recipient);

					// display mail sending process
                    counter++;
                    if (recipient.Skip.GetValueOrDefault())
                        Console.ForegroundColor = ConsoleColor.DarkGray;
					else
                        Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"({counter}) {recipient.Company?.ToUpper()} {recipient.Name}");
                    Console.WriteLine($"Mail: {recipient.Address}");
					Console.WriteLine($"Subject: {mailMessage.Subject}");
					Console.WriteLine($"Template: {(!string.IsNullOrEmpty(recipient.MailBodyTemplateFilePath) ? recipient.MailBodyTemplateFilePath : mailDaemonService.MailProfile.MailBodyTemplateFilePath)}");

                    if (recipient.Skip.GetValueOrDefault())
                        recipientReportInfo.AppendLine("<div style=\"color: #999\">");
                    recipientReportInfo.AppendLine($"({counter}) {recipient.Company?.ToUpper()} {recipient.Name} <a href=\"mailto:{recipient.Address}\">{recipient.Address}</a>");
					recipientReportInfo.AppendLine($"<div>Subject: {mailMessage.Subject}</div>");
					if (!string.IsNullOrEmpty(recipient.MailBodyTemplateFilePath) && recipient.MailBodyTemplateFilePath != mailDaemonService.MailProfile.MailBodyTemplateFilePath)
    					recipientReportInfo.AppendLine($"<div>Template: {recipient.MailBodyTemplateFilePath}</div>");

                    // recipient related attachments use at first
                    if (recipient.Attachments != null)
                    {
                        foreach (var attachment in recipient.Attachments)
                        {
                            if (File.Exists(attachment.Path))
                            {
                                Console.WriteLine($"\tAttachment: \"{attachment.Path}\"");
                                recipientReportInfo.AppendLine($"<div style=\"padding-left: 40px\">Attachment: \"{attachment.Path}\"</div>");
                            }
                            else
                            {
                                DisplayWarningMessage($"\tAttachment: file \"{attachment.Path}\" not exists.");
                                recipientReportInfo.AppendLine($"<div style=\"padding-left: 40px; color: #aa0000\">Attachment: file \"{attachment.Path}\" not exists.</div>");
                            }
                        }
                    }

                    // attachments
                    if (mailDaemonService.MailProfile.Attachments != null)
					{
						foreach (var attachment in mailDaemonService.MailProfile.Attachments)
						{
							if (File.Exists(attachment.Path))
							{
								Console.WriteLine($"\tAttachment: \"{attachment.Path}\"");
								recipientReportInfo.AppendLine($"<div style=\"padding-left: 40px\">Attachment: \"{attachment.Path}\"</div>");
							}
							else
							{
								DisplayWarningMessage($"\tAttachment: file \"{attachment.Path}\" not exists.");
								recipientReportInfo.AppendLine($"<div style=\"padding-left: 40px\">Attachment: file \"{attachment.Path}\" not exists.</div>");
							}
						}
					}

                    if (recipient.Skip.GetValueOrDefault())
                    {
                        recipientReportInfo.AppendLine("--- Skipped ---");
                        recipientReportInfo.AppendLine("</div>");
                    }

                    if (mailDaemonService.SendDemo)
					{
						Console.ForegroundColor = ConsoleColor.Cyan;
						Console.WriteLine($"--- Send demo to sender address: {mailDaemonService.MailProfile.Sender.Address} ---");
						Console.ResetColor();
                    }

                    if (mailDaemonService.GeneratePreview)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"--- Create file \"{recipient.Address}.html\" with preview ---");
                        Console.ResetColor();
                        try
                        {
                            var fileNamePrefix = "";
							if (recipient.Skip.GetValueOrDefault())
                                fileNamePrefix = "(skipped)_";
                            var previewFilePath = Path.Combine(PreviewsDirPath, $"{fileNamePrefix}{recipient.Address}{Path.GetExtension(recipient.MailBodyTemplateFilePath)}");
                            File.WriteAllText(previewFilePath, mailMessage.Body);
                        }
                        catch (Exception ex)
                        {
                            DisplayErrorMessage(ex.Message);
                        }
                    }

                    if (!mailDaemonService.JustValidate)
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
				Thread.Sleep(mailDaemonService.SendSleep);
			}

            var report = GenerateReport(mailDaemonService, recipientsReport);
            try
            {
                var reportFilePath = Path.Combine(ReportsDirPath, $"report_{Path.GetFileName(mailDaemonService.MailProfileFilename)}_{DateTime.Now:dd.MM.yyyy_HH-mm}.html");
                File.WriteAllText(reportFilePath, report);
            }
            catch (Exception ex)
            {
				DisplayWarningMessage(ex.Message);
                Console.Write("Press any key to continue...");
                Console.ReadKey();
            }

            if (!mailDaemonService.JustValidate)
			{
				// send status report to sender
				try
				{
					var mailMessage = new MailMessage();
					mailMessage.To.Add(mailDaemonService.GetMailAddress(mailDaemonService.Operator.Address, mailDaemonService.Operator.Name));
					mailMessage.From = mailDaemonService.GetMailAddress(mailDaemonService.MailProfile.Sender.Address, mailDaemonService.MailProfile.Sender.Name);
					mailMessage.ReplyToList.Add(mailMessage.From);
					mailMessage.Headers.Add("Reply-To", mailDaemonService.MailProfile.Sender.Address);
					mailMessage.Subject = "Mail Daemon: mails has been sent";
					mailMessage.SubjectEncoding = Encoding.UTF8;
					mailMessage.IsBodyHtml = true;
					mailMessage.BodyEncoding = Encoding.UTF8;
                    mailMessage.Body = report;

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

			if (mailDaemonService.SendDemo)
                WaitForExit();
		}

        private static string GenerateReport(MailDaemonService mailDaemonService, StringBuilder recipientsReport)
        {
            var report = new StringBuilder();
            report.AppendLine("<!DOCTYPE html>");
            report.AppendLine("<html>");
            report.AppendLine("<head>");
            report.AppendLine("<meta charset=\"utf-8\" />");
            report.AppendLine("<title>Mail Daemon report</title>");
            report.AppendLine("</head>");
            report.AppendLine("<body>");
            report.AppendLine($"<div>{mailDaemonService.MailProfile.Recipients.Count} mails has been sent.</div>");
            report.AppendLine($"<div>Mail profile: \"{mailDaemonService.MailProfileFilename}\"</div>");
            report.AppendLine($"<div>Mail template: \"{mailDaemonService.MailProfile.MailBodyTemplateFilePath}\"</div>");
            report.AppendLine("<br/>");
            report.AppendLine($"<div><strong>Recipients:</strong></div>");
            report.AppendLine($"<div>{recipientsReport}</div>");
            report.AppendLine("</body>");
            report.AppendLine("</html>");

            return report.ToString();
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

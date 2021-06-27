using System.Collections.Generic;
using Newtonsoft.Json;

namespace BlackNight.MailDaemon
{
	public class MailProfile
	{
		[JsonProperty("sender")]
		public SenderInfo Sender { get; set; }

		[JsonProperty("recipients")]
		public List<RecipientInfo> Recipients { get; set; }

		[JsonProperty("subject")]
		public string Subject { get; set; }

		/// <summary>
		/// Path to mail template file.
		/// </summary>
		[JsonProperty("template")]
		public string MailBodyTemplate { get; set; }

		public string MailBody { get; set; }

		[JsonProperty("attachments")]
		public List<AttachmentInfo> Attachments { get; set; }
	}
}

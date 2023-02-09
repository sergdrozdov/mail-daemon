using System.Collections.Generic;
using Newtonsoft.Json;

namespace BlackNight.MailDaemon.Core
{
	public class RecipientInfo
	{
		[JsonProperty("address")]
		public string Address { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		/// <summary>
		/// Custom subject for this recipient.
		/// </summary>
		[JsonProperty("subject")]
		public string Subject { get; set; }

		/// <summary>
		/// Path to mail template file.
		/// </summary>
		[JsonProperty("template")]
		public string MailBodyTemplate { get; set; }

		public string MailBody { get; set; }

		[JsonProperty("company")]
		public string Company { get; set; }

        [JsonProperty("contact_person")]
        public string ContactPerson { get; set; }

        [JsonProperty("language")]
		public string Language { get; set; }

		[JsonProperty("attachments")]
		public List<AttachmentInfo> Attachments { get; set; }

        /// <summary>
        /// If true, the mail is not sending to the recipient.
        /// </summary>
        [JsonProperty("skip")]
		public bool? Skip { get; set; }
	}
}

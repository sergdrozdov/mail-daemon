﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace MailDaemon.Core
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
        public string MailBodyTemplateFilePath { get; set; } = "";

        /// <summary>
        /// Template content.
        /// </summary>
        public string MailBody { get; set; }

		[JsonProperty("attachments")]
		public List<AttachmentInfo> Attachments { get; set; }

        /// <summary>
        /// List of data to replace text in the mail body template.
        /// </summary>
        [JsonProperty("replace")]
        public Dictionary<string, string> Replace { get; set; }
	}
}

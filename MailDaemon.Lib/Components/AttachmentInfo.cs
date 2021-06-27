using Newtonsoft.Json;

namespace BlackNight.MailDaemon
{
	public class AttachmentInfo
	{
		/// <summary>
		/// File full path.
		/// </summary>
		[JsonProperty("path")]
		public string Path { get; set; }

		/// <summary>
		/// Custom filename for recipient.
		/// </summary>
		[JsonProperty("filename")]
		public string FileName { get; set; }
	}
}

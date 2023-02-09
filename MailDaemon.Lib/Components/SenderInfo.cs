using Newtonsoft.Json;

namespace BlackNight.MailDaemon.Core
{
	public class SenderInfo
	{
		/// <summary>
		/// Mail address.
		/// </summary>
		[JsonProperty("address")]
		public string Address { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }
	}
}

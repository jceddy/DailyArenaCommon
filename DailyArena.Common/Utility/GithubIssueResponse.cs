using Newtonsoft.Json;

namespace DailyArena.Common.Utility
{
	/// <summary>
	/// Class that represents the response from the Github Issue creation operation.
	/// </summary>
	public class GithubIssueResponse
	{
		/// <summary>
		/// Gets or sets the issue number.
		/// </summary>
		[JsonProperty(PropertyName = "number")]
		public int Number { get; set; }

		/// <summary>
		/// Gets or sets the issue creation state ("exists" or "created").
		/// </summary>
		[JsonProperty(PropertyName = "state")]
		public string State { get; set; }
	}
}

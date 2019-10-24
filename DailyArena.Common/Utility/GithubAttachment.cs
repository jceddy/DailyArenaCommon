using Newtonsoft.Json;

namespace DailyArena.Common.Utility
{
	/// <summary>
	/// A class that represents a file attachment for a Github issue.
	/// </summary>
	public class GithubAttachment
	{
		/// <summary>
		/// Gets or sets the attachment's name (short filename).
		/// </summary>
		[JsonProperty(PropertyName = "name")]
		public string Name { get; private set; }

		/// <summary>
		/// Gets or sets the content of the attachment file.
		/// </summary>
		[JsonProperty(PropertyName = "content")]
		public string Content { get; private set; }

		/// <summary>
		/// GithubAttachment constructor.
		/// </summary>
		/// <param name="name">The attachment's name (short filename).</param>
		/// <param name="content">The content of the attachment file.</param>
		public GithubAttachment(string name, string content)
		{
			Name = name;
			Content = content;
		}
	}
}

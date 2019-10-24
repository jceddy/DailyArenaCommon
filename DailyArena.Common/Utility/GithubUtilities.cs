using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;

namespace DailyArena.Common.Utility
{
	/// <summary>
	/// Class for interacting with Github via the API.
	/// </summary>
	public static class GithubUtilities
	{
		/// <summary>
		/// Create an issue on Github if it doesn't already exist.
		/// </summary>
		/// <param name="productHeader">The product header to use on Github API calls.</param>
		/// <param name="owner">The owner of the repo to create the issue for.</param>
		/// <param name="repo">The name of the repo to create the issue for.</param>
		/// <param name="title">The title of the new issue to create.</param>
		/// <param name="body">The body of the new issue to create.</param>
		/// <param name="labels">An array of labels to attach to the new issue.</param>
		/// <param name="fingerprint">The user's current unique identifier.</param>
		/// <param name="attachments">Array of file attachment information for the issue.</param>
		/// <param name="exception">Any exception that is caught while attempting to create the issue.</param>
		/// <returns>An object creating the created or existing number, as well as a string determining whether a new issue was created.</returns>
		public static GithubIssueResponse CreateNewIssue(string productHeader, string owner, string repo, string title, string body, string[] labels, Guid fingerprint,
			GithubAttachment[] attachments, out Exception exception)
		{
			exception = null;
			GithubIssueResponse responseObject = null;

			try
			{
				NameValueCollection data = new NameValueCollection()
				{
					{ "product_header", productHeader },
					{ "owner", owner },
					{ "repo", repo },
					{ "title", title },
					{ "fingerprint", fingerprint.ToString() },
					{ "body", body },
					{ "labels", string.Join(",", labels) },
					{ "attachments", JsonConvert.SerializeObject(attachments) }
				};
				string response = WebUtilities.UploadValues("https://clans.dailyarena.net/create_github_issue.php", data);

				responseObject = JsonConvert.DeserializeObject<GithubIssueResponse>(response);
			}
			catch(Exception e)
			{
				exception = e;
			}

			return responseObject;
		}
	}
}

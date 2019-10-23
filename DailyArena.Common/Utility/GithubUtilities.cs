using Octokit;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
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
		/// <param name="labels">A list of labels to attach to the new issue.</param>
		/// <param name="mainWindow">The calling Window.</param>
		/// <param name="credentialDialogTitle">The title for the github credential dialog.</param>
		/// <param name="credentialDialogInstruction">The main instructions for the github credential dialog.</param>
		/// <param name="credentialDialogContent">The detailed content for the github credential dialog.</param>
		/// <param name="exception">Any exception that is caught while attempting to create the issue.</param>
		public static void CreateNewIssue(string productHeader, string owner, string repo, string title, string body, List<string> labels, Window mainWindow,
			string credentialDialogTitle, string credentialDialogInstruction, string credentialDialogContent, out Exception exception)
		{
			exception = null;

			try
			{
				CredentialDialog credentialDialog = new CredentialDialog
				{
					Target = "github.com",
					WindowTitle = credentialDialogTitle,
					MainInstruction = credentialDialogInstruction,
					Content = credentialDialogContent
				};
				bool ok = false;
				mainWindow.Dispatcher.Invoke(() => { ok = credentialDialog.ShowDialog(mainWindow); });

				if (ok)
				{
					var client = new GitHubClient(new ProductHeaderValue(productHeader))
					{
						Credentials = new Credentials(credentialDialog.UserName, credentialDialog.Password)
					};
					var issueRequest = new RepositoryIssueRequest
					{
						Filter = IssueFilter.All,
						State = ItemStateFilter.Open
					};
					foreach (var label in labels)
					{
						issueRequest.Labels.Add(label);
					}
					var issues = client.Issue.GetAllForRepository(owner, repo).Result;

					bool createIssue = true;
					foreach (var issue in issues)
					{
						if (issue.Title == title)
						{
							createIssue = false;
							break;
						}
					}

					if (createIssue)
					{
						var newIssue = new NewIssue(title)
						{
							Body = body
						};
						foreach (var label in labels)
						{
							newIssue.Labels.Add(label);
						}
						var issue = client.Issue.Create(owner, repo, newIssue).Result;
					}
				}
			}
			catch(Exception e)
			{
				exception = e;
			}
		}
	}
}

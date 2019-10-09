using System;
using System.Net;

namespace DailyArena.Common.Utility
{
	/// <summary>
	/// Wrapper class for WebClient that allows setting request timeouts.
	/// </summary>
	public class WebClientEx : WebClient
	{
		/// <summary>
		/// The timeout for requests on this client (in milliseconds).
		/// </summary>
		/// <remarks>Default value comes from WebUtilities.Timeout</remarks>
		public int Timeout { get; set; } = WebUtilities.Timeout;

		/// <summary>
		/// Get a web request for a specific Uri.
		/// </summary>
		/// <param name="address">The Uri for the web request.</param>
		/// <returns>The web request for the specified Uri.</returns>
		protected override WebRequest GetWebRequest(Uri address)
		{
			var request = base.GetWebRequest(address);
			request.Timeout = Timeout;
			return request;
		}
	}
}

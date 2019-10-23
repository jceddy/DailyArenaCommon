using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace DailyArena.Common.Utility
{
	/// <summary>
	/// A utility class containing useful methods for interacting with Web resources.
	/// </summary>
	public static class WebUtilities
	{
		/// <summary>
		/// The timeout for web requests made by this class (in milliseconds).
		/// </summary>
		/// <remarks>Default value is 200000 (200 seconds).</remarks>
		public static int Timeout { get; set; } = 200000;

		/// <summary>
		/// Fetch a specified url and return a string with its contents.
		/// </summary>
		/// <param name="url">The url to fetch.</param>
		/// <param name="ignoreWebException">If this is true, ignore WebExceptions (return null on failure instead). [Defaults to false]</param>
		/// <returns>A string with the contents of the resource at the specified url.</returns>
		public static string FetchStringFromUrl(string url, bool ignoreWebException = false)
		{
			return FetchStringFromUrl(url, ignoreWebException, out List<WebException> exceptions);
		}

		/// <summary>
		/// Fetch a specified url and return a string with its contents.
		/// </summary>
		/// <param name="url">The url to fetch.</param>
		/// <param name="ignoreWebException">If this is true, ignore WebExceptions (return null on failure instead).</param>
		/// <param name="exceptions">out parameter that holds a list of WebExceptions that were caught</param>
		/// <returns>A string with the contents of the resource at the specified url.</returns>
		public static string FetchStringFromUrl(string url, bool ignoreWebException, out List<WebException> exceptions)
		{
			exceptions = new List<WebException>();

			var request = WebRequest.Create(url);
			request.Method = "GET";
			request.Timeout = Timeout;
			try
			{
				using (var response = request.GetResponse())
				{
					using (Stream responseStream = response.GetResponseStream())
					using (StreamReader responseReader = new StreamReader(responseStream))
					{
						return responseReader.ReadToEnd();
					}
				}
			}
			catch (WebException e)
			{
				exceptions.Add(e);
			}

			// if we got here, that means the original request timed out...we will give it one more try with a longer time-out value before we give up completely
			request = WebRequest.Create(url);
			request.Method = "GET";
			request.Timeout = Timeout * 2;
			try
			{
				using (var response = request.GetResponse())
				{
					using (Stream responseStream = response.GetResponseStream())
					using (StreamReader responseReader = new StreamReader(responseStream))
					{
						return responseReader.ReadToEnd();
					}
				}
			}
			catch(WebException e)
			{
				exceptions.Add(e);
				if (ignoreWebException)
				{
					return null;
				}
				throw;
			}
		}

		/// <summary>
		/// Upload data to a specified url.
		/// </summary>
		/// <param name="url">The url to upload data to.</param>
		/// <param name="data">The data to upload to the url.</param>
		/// <param name="method">The method to use (defaults to "POST").</param>
		/// <param name="ignoreWebException">If this is true, ignore WebExceptions (return null on failure instead).</param>
		/// <returns>The string response from the server.</returns>
		public static string UploadValues(string url, NameValueCollection data, string method = "POST", bool ignoreWebException = false)
		{
			return UploadValues(url, data, method, ignoreWebException, out List<WebException> exceptions);
		}

		/// <summary>
		/// Upload data to a specified url.
		/// </summary>
		/// <param name="url">The url to upload data to.</param>
		/// <param name="data">The data to upload to the url.</param>
		/// <param name="method">The method to use (defaults to "POST").</param>
		/// <param name="ignoreWebException">If this is true, ignore WebExceptions (return null on failure instead).</param>
		/// <param name="exceptions">out parameter that holds a list of WebExceptions that were caught</param>
		/// <returns>The string response from the server.</returns>
		public static string UploadValues(string url, NameValueCollection data, string method, bool ignoreWebException, out List<WebException> exceptions)
		{
			exceptions = new List<WebException>();

			using (WebClientEx wc = new WebClientEx())
			{
				try
				{
					return Encoding.UTF8.GetString(wc.UploadValues(url, method, data));
				}
				catch (WebException e)
				{
					exceptions.Add(e);
				}
			}

			// if we got here, that means the original request timed out...we will give it one more try with a longer time-out value before we give up completely
			using (WebClientEx wc = new WebClientEx() { Timeout = Timeout * 2 })
			{
				try
				{
					return Encoding.UTF8.GetString(wc.UploadValues(url, method, data));
				}
				catch(WebException e)
				{
					exceptions.Add(e);
					if (ignoreWebException)
					{
						return null;
					}
					throw;
				}
			}
		}
	}
}

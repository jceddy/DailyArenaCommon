using Serilog;
using System.Management;
using System.Windows;

namespace DailyArena.Common.Extensions
{
	/// <summary>
	/// Class that contains static extension methods for other classes.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// The logger object.
		/// </summary>
		static ILogger _logger;

		/// <summary>
		/// Static constructor, stores a reference to the application's logger.
		/// </summary>
		static Extensions()
		{
			IApp application = (IApp)Application.Current;
			_logger = application.Logger;
		}
	}
}

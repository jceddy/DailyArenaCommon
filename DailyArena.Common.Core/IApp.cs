using Serilog;

namespace DailyArena.Common.Core
{
	/// <summary>
	/// Interface for Daily Arena applications. Requires the application to host a Serilog logger.
	/// </summary>
	public interface IApp
	{
		/// <summary>
		/// Gets the Serilog Logger for the application.
		/// </summary>
		ILogger Logger { get; set; }
	}
}

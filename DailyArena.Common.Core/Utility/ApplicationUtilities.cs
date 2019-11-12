namespace DailyArena.Common.Core.Utility
{
	/// <summary>
	/// Static class used as an interface to applications running under various frameworks.
	/// </summary>
	public static class ApplicationUtilities
	{
		public static IApp CurrentApp { get; set; }
	}
}

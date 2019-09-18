using System;
using System.Collections.Generic;
using System.Text;

namespace DailyArena.Common.Bindable
{
	/// <summary>
	/// A boolean value that Xaml fields can easily bind to.
	/// </summary>
	public class BindableBool : Bindable<bool>
	{
		/// <summary>
		/// Gets the opposite of the internal boolan value.
		/// </summary>
		public bool NotValue
		{
			get
			{
				return !_value;
			}
		}

		/// <summary>
		/// Method called when Value changed, to propagate the new value.
		/// </summary>
		protected override void RaiseValuePropertyChanged()
		{
			RaisePropertyChanged(() => NotValue);
			base.RaiseValuePropertyChanged();
		}
	}
}

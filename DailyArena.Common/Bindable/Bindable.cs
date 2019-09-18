using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace DailyArena.Common.Bindable
{
	/// <summary>
	/// A generic class for objects that Xaml fields can easily bind to.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class Bindable<T> : INotifyPropertyChanged, IEquatable<T> where T : IEquatable<T>
	{
		/// <summary>
		/// The internal value.
		/// </summary>
		protected T _value;

		/// <summary>
		/// Gets or sets the internal value.
		/// </summary>
		public T Value
		{
			get
			{
				return _value;
			}
			set
			{
				if(!Equals(value))
				{
					_value = value;
					RaiseValuePropertyChanged();
				}
			}
		}

		/// <summary>
		/// Method called when Value changed, to propagate the new value.
		/// </summary>
		protected virtual void RaiseValuePropertyChanged()
		{
			RaisePropertyChanged(() => Value);
		}

		#region INotifyPropertyChanged Members

		/// <summary>
		/// Event handler to trigger when the internal string value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raise the propery changed event.
		/// </summary>
		/// <param name="property">The name of the property that changed.</param>
		public void RaisePropertyChanged(string property)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
		}

		/// <summary>
		/// Raise the property changed event for a propery expression.
		/// </summary>
		/// <typeparam name="T">The type of the expression.</typeparam>
		/// <param name="propertyExpression">The expression to raise the changed event for.</param>
		public void RaisePropertyChanged<PType>(Expression<Func<PType>> propertyExpression)
		{
			if (propertyExpression == null)
			{
				return;
			}

			var handler = PropertyChanged;

			if (handler != null)
			{
				MemberExpression body = propertyExpression.Body as MemberExpression;
				if (body != null)
					handler(this, new PropertyChangedEventArgs(body.Member.Name));
			}
		}

		/// <summary>
		/// Checks for equality of the internal value to another object of the same type.
		/// </summary>
		/// <param name="other">The other object to compare equality of the internal value to.</param>
		/// <returns>True if the interal value is equal to the passed object; false otherwise.</returns>
		public bool Equals(T other)
		{
			return EqualityComparer<T>.Default.Equals(_value, other);
		}

		#endregion

		/// <summary>
		/// Override of the object equality method.
		/// </summary>
		/// <param name="obj">The object to check for equality.</param>
		/// <returns>True if the passed object is equal to this one.</returns>
		public override bool Equals(object obj)
		{
			if(obj is T)
			{
				return this == (T)obj;
			}
			else if(obj is Bindable<T>)
			{
				return this == (Bindable<T>)obj;
			}
			return false;
		}

		/// <summary>
		/// Override of the object GetHashCode method.
		/// </summary>
		/// <returns>The hash code of the internal value, or 0 if that value is null.</returns>
		public override int GetHashCode()
		{
			return _value == null ? 0 : _value.GetHashCode();
		}

		/// <summary>
		/// == operator to compare Bindable&lt;<typeparamref name="T"/>&gt; to <typeparamref name="T"/>
		/// </summary>
		/// <param name="a">A Bindable&lt;<typeparamref name="T"/>&gt; object</param>
		/// <param name="b">A <typeparamref name="T"/> value</param>
		/// <returns>True if a's interal value is equal to b; false otherwise</returns>
		public static bool operator ==(Bindable<T> a, T b) => a.Equals(b);

		/// <summary>
		/// != operator to compare Bindable&lt;<typeparamref name="T"/>&gt; to <typeparamref name="T"/>
		/// </summary>
		/// <param name="a">A Bindable&lt;<typeparamref name="T"/>&gt; object</param>
		/// <param name="b">A <typeparamref name="T"/> value</param>
		/// <returns>True if a's interal value is not equal to b; false otherwise</returns>
		public static bool operator !=(Bindable<T> a, T b) => !a.Equals(b);

		/// <summary>
		/// == operator to compare <typeparamref name="T"/> to Bindable&lt;<typeparamref name="T"/>&gt;
		/// </summary>
		/// <param name="a">A <typeparamref name="T"/> value</param>
		/// <param name="b">A Bindable&lt;<typeparamref name="T"/>&gt; object</param>
		/// <returns>True if b's interal value is equal to a; false otherwise</returns>
		public static bool operator ==(T a, Bindable<T> b) => b.Equals(a);

		/// <summary>
		/// != operator to compare <typeparamref name="T"/> to Bindable&lt;<typeparamref name="T"/>&gt;
		/// </summary>
		/// <param name="a">A <typeparamref name="T"/> value</param>
		/// <param name="b">A Bindable&lt;<typeparamref name="T"/>&gt; object</param>
		/// <returns>True if b's interal value is not equal to a; false otherwise</returns>
		public static bool operator !=(T a, Bindable<T> b) => !b.Equals(a);

		/// <summary>
		/// == operator to compare Bindable&lt;<typeparamref name="T"/>&gt; to Bindable&lt;<typeparamref name="T"/>&gt;
		/// </summary>
		/// <param name="a">A Bindable&lt;<typeparamref name="T"/>&gt; object</param>
		/// <param name="b">A Bindable&lt;<typeparamref name="T"/>&gt; object</param>
		/// <returns>True if a's interal value is equal to b's internal value; false otherwise</returns>
		public static bool operator ==(Bindable<T> a, Bindable<T> b) => a.Equals(b.Value);

		/// <summary>
		/// == operator to compare Bindable&lt;<typeparamref name="T"/>&gt; to Bindable&lt;<typeparamref name="T"/>&gt;
		/// </summary>
		/// <param name="a">A Bindable&lt;<typeparamref name="T"/>&gt; object</param>
		/// <param name="b">A Bindable&lt;<typeparamref name="T"/>&gt; object</param>
		/// <returns>True if a's interal value is not equal to b's internal value; false otherwise</returns>
		public static bool operator !=(Bindable<T> a, Bindable<T> b) => !a.Equals(b.Value);
	}
}

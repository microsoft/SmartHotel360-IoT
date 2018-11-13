using System.Threading;

namespace SmartHotel.Devices.RoomDevice.Models
{
	public abstract class SensorInfo
	{
		public object CurrentValue { get; }
		public object LastValueSent { get; }
		public abstract bool IsCurrentValueDifferent();
		public abstract void UpdateCurrentValue( object newCurrentValue );
		public abstract void UpdateLastValueSent( object newLastValueSent );
	}

	public class SensorInfo<T> : SensorInfo
	{
		public SensorInfo(T currentValue, T lastValueSent)
		{
			_currentValue = currentValue;
			_lastValueSent = lastValueSent;
		}

		private object _currentValue;

		public new T CurrentValue => (T)_currentValue;

		private object _lastValueSent;
		public new T LastValueSent => (T)_lastValueSent;

		public override bool IsCurrentValueDifferent()
		{
			return Equals( CurrentValue, LastValueSent );
		}

		public override void UpdateCurrentValue( object newCurrentValue )
		{
			Interlocked.Exchange( ref _currentValue, newCurrentValue );
		}

		public override void UpdateLastValueSent( object newLastValueSent )
		{
			Interlocked.Exchange( ref _lastValueSent, newLastValueSent );
		}
	}
}

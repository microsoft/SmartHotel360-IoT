using System.Threading;

namespace SmartHotel.Devices.RoomDevice.Models
{
	public class SensorInfo
	{
		public SensorInfo(object currentValue, object lastValueSent)
		{
			_currentValue = currentValue;
			_lastValueSent = lastValueSent;
		}

		private object _currentValue;
		private object _lastValueSent;

		public bool IsCurrentValueDifferent()
		{
			return !Equals( _currentValue, _lastValueSent );
		}

		public object GetCurrentValue()
		{
			return _currentValue;
		}

		public void UpdateLastValueSentWithCurrentValue()
		{
			Interlocked.Exchange( ref _lastValueSent, _currentValue );
		}

		public void UpdateCurrentValue( object newCurrentValue )
		{
			Interlocked.Exchange( ref _currentValue, newCurrentValue );
		}
	}
}

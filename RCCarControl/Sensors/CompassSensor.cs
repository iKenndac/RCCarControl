using System;

namespace RCCarControl {
	public class CompassSensor : Sensor {

		private int _degrees;

		public CompassSensor() {
			_degrees = 0;
		}

		public int Degrees {
			get { return _degrees; }
			internal set {
				if (value != _degrees) {
					_degrees = value;
					ReadingTime = DateTime.Now;
					NotifyReadingChanged(new ReadingChangedEventArgs());
				}
			}
		}
		
		public override String DisplayReading {
			get { return string.Format("{0}°", Degrees); }
		}
		
		String Name {
			get { return "Compass Sensor"; }
		}
	}
}


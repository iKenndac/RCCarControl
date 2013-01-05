using System;

namespace RCCarControl {

	public class UltrasonicSensor : Sensor {

		private int _distanceReadingCM = 0; 

		public UltrasonicSensor () {
			_distanceReadingCM = 0;
		}

		public int DistanceReadingCM { 
			get { return _distanceReadingCM; }
			internal set {
				if (value != _distanceReadingCM) {
					_distanceReadingCM = value;
					NotifyReadingChanged(new ReadingChangedEventArgs());
				}
			}
		}

		public override String DisplayReading {
			get { return string.Format("{0}cm", DistanceReadingCM); }
		}

		String Name {
			get { return "Ultrasonic Sensor"; }
		}
	}
}


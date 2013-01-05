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

		String Name {
			get { return "Ultrasonic Sensor"; }
		}
	}
}


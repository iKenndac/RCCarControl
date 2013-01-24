using System;

namespace RCCarCore {
	public class AccelerometorSensor : Sensor {

		private double _x, _y, _z;

		public AccelerometorSensor() {
			_x = 0.0;
			_y = 0.0;
			_z = 0.0;
		}

		internal void SetValues(double x, double y, double z) {
			_x = x;
			_y = y;
			_z = z;
			ReadingTime = DateTime.Now;
			NotifyReadingChanged(new ReadingChangedEventArgs());
		}

		public double X { get { return _x; }}
		public double Y { get { return _y; }}
		public double Z { get { return _z; }}

		public override String DisplayReading {
			get { return string.Format("X: {0:0.00}g, Y: {1:0.00}g, Z:{2:0.00}g", X, Y, Z); }
		}

		String Name {
			get { return "Accelerometer"; }
		}
	}
}


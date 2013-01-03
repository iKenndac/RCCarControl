using System;

namespace RCCarControl
{
	public class ReadingChangedEventArgs : EventArgs {}

	public class Sensor
	{
		public Sensor ()
		{
		}

		public delegate void SensorReadingChangedEventHandler(object sender, ReadingChangedEventArgs e);
		public event SensorReadingChangedEventHandler ReadingChanged;

		String Name {
			get { return "Unknown Sensor"; }
		}

		private void NotifyReadingChanged(ReadingChangedEventArgs e) {
			if (ReadingChanged != null) ReadingChanged(this, e);
		}

	}
}


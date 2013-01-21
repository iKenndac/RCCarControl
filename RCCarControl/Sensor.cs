using System;

namespace RCCarControl
{
	public class ReadingChangedEventArgs : EventArgs {}

	public class Sensor
	{
		public Sensor ()
		{
		}

		public delegate void SensorReadingChangedEventHandler(Sensor sender, ReadingChangedEventArgs e);
		public event SensorReadingChangedEventHandler ReadingChanged;

		String Name {
			get { return "Unknown Sensor"; }
		}

		public virtual String DisplayReading {
			get { return "Unknown"; }
		}

		public DateTime ReadingTime {
			get;
			protected set;
		}

		protected void NotifyReadingChanged(ReadingChangedEventArgs e) {
			if (ReadingChanged != null) ReadingChanged(this, e);
		}

	}
}


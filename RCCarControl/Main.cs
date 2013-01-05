using System;
using System.Threading;

namespace RCCarControl
{
	class MainClass
	{

		static ManualResetEvent mre = new ManualResetEvent(false);

		public static void Main (string[] args) {

			// Start a thread or two to do some work...
			ThreadPool.QueueUserWorkItem(o => BackgroundWork());
			// Block until our ManualResetEvent is set
			mre.WaitOne();
		}

		static SerialRCCarHardwareInterface Car { get; set; }

		static void BackgroundWork() {

			Car = new SerialRCCarHardwareInterface("/dev/cu.usbmodemfa131");

			Car.RearUltrasonicSensor.ReadingChanged += delegate(Sensor sender, ReadingChangedEventArgs e) {
				Console.Out.WriteLine("Rear Sensor changed to {0}.", sender.DisplayReading);
			};

			Car.FrontUltrasonicSensors[(int)SerialRCCarHardwareInterface.UltrasonicSensorIndex.FrontLeft].ReadingChanged += delegate(Sensor sender, ReadingChangedEventArgs e) {
				Console.Out.WriteLine("Front Left Sensor changed to {0}.", sender.DisplayReading);
			};
			
			Car.FrontUltrasonicSensors[(int)SerialRCCarHardwareInterface.UltrasonicSensorIndex.FrontRight].ReadingChanged += delegate(Sensor sender, ReadingChangedEventArgs e) {
				Console.Out.WriteLine("Front Right Sensor changed to {0}.", sender.DisplayReading);
			};
			
			Car.FrontUltrasonicSensors[(int)SerialRCCarHardwareInterface.UltrasonicSensorIndex.FrontMiddle].ReadingChanged += delegate(Sensor sender, ReadingChangedEventArgs e) {
				Console.Out.WriteLine("Front Middle Sensor changed to {0}.", sender.DisplayReading);
			};

			// When we're done, we "set" the wait handle, which allows the program to shutdown...
			//mre.Set();
		}
	}
}

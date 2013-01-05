using System;
using System.Threading;
using System.Net;

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
		static RCCarHTTPServer Server { get; set; }

		static void BackgroundWork() {

			Car = new SerialRCCarHardwareInterface("/dev/cu.usbmodemfa131");
			Server = new RCCarHTTPServer(Car);

			Car.RearUltrasonicSensor.ReadingChanged += delegate(Sensor sender, ReadingChangedEventArgs e) {
				Console.Out.WriteLine("Rear Sensor changed to {0}.", sender.DisplayReading);
			};

			Car.FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontLeft].ReadingChanged += delegate(Sensor sender, ReadingChangedEventArgs e) {
				Console.Out.WriteLine("Front Left Sensor changed to {0}.", sender.DisplayReading);
			};
			
			Car.FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontRight].ReadingChanged += delegate(Sensor sender, ReadingChangedEventArgs e) {
				Console.Out.WriteLine("Front Right Sensor changed to {0}.", sender.DisplayReading);
			};
			
			Car.FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontMiddle].ReadingChanged += delegate(Sensor sender, ReadingChangedEventArgs e) {
				Console.Out.WriteLine("Front Middle Sensor changed to {0}.", sender.DisplayReading);
			};

			if (HttpListener.IsSupported)
				Console.Out.WriteLine("Yay!");

			// When we're done, we "set" the wait handle, which allows the program to shutdown...
			//mre.Set();
		}
	}
}

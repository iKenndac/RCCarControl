using System;
using System.Threading;
using System.Net;

namespace RCCarControl
{
	class MainClass
	{

		static ManualResetEvent mre = new ManualResetEvent(false);
		static string[] applicationArguments;

		public static void Main (string[] args) {

			applicationArguments = args;
			// Start a thread or two to do some work...
			ThreadPool.QueueUserWorkItem(o => BackgroundWork());
			// Block until our ManualResetEvent is set
			mre.WaitOne();
		}

		static SerialRCCarHardwareInterface Car { get; set; }
		static RCCarHTTPServer Server { get; set; }

		static void BackgroundWork() {

			bool shouldStartHTTPServer = false;
			bool shouldPrintDistanceChanges = false;

			foreach (string arg in applicationArguments) {
				if (arg == "-httpserver")
					shouldStartHTTPServer = true;
				if (arg == "-logdistance")
					shouldPrintDistanceChanges = true;
			}

			Car = new SerialRCCarHardwareInterface("/dev/cu.usbmodemfa131");

			if (shouldStartHTTPServer) {
				int port = 8080;
				Server = new RCCarHTTPServer(Car, port);
				Console.Out.WriteLine("Started HTTP server on port {0}.", port);
			}

			if (shouldPrintDistanceChanges) {
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
			}

			// When we're done, we "set" the wait handle, which allows the program to shutdown...
			//mre.Set();
		}
	}
}

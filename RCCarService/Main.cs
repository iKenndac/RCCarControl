using System;
using System.Threading;
using System.Net;
using System.IO;
using RCCarCore;

namespace RCCarService {
	class MainClass {
		
		static ManualResetEvent mre = new ManualResetEvent(false);
		static string[] applicationArguments;
		
		public static void Main (string[] args) {
			
			applicationArguments = args;
			// Start a thread or two to do some work...
			ThreadPool.QueueUserWorkItem(o => BackgroundWork());
			// Block until our ManualResetEvent is set
			mre.WaitOne();
		}

		static SerialCarHardwareInterface Car { get; set; }
		static CarHTTPServer Server { get; set; }
		static CarEventLoop Loop { get; set; }

		static void BackgroundWork() {
			
			bool shouldStartHTTPServer = false;
			bool shouldPrintDistanceChanges = false;
			bool shouldPrintAccelerometerChanges = false;
			int httpPort = 8080;
			string serialPortPath = null;
			
			// ---- Command line argument parsing
			
			for (int argIndex = 0; argIndex < applicationArguments.Length; argIndex++) {
				
				string arg = applicationArguments[argIndex];
				
				if (arg == "-httpserver") shouldStartHTTPServer = true;
				if (arg == "-logdistance") shouldPrintDistanceChanges = true;
				if (arg == "-logaccel") shouldPrintAccelerometerChanges = true;
				
				if (arg == "-httpport") {
					argIndex++;
					try {
						string portString = applicationArguments[argIndex];
						httpPort = Convert.ToInt32(portString);
					} catch {
						Console.Out.WriteLine("Fatal: Invalid HTTP port.");
						mre.Set();
						return;
					}
				}
				
				if (arg == "-serialport") {
					argIndex++;
					try {
						serialPortPath = applicationArguments[argIndex];
					} catch {}
				}
				
			}
			
			if (serialPortPath == null) {
				Console.Out.WriteLine("Fatal: No serial port given. Set with -serialport.");
				mre.Set();
				return;
			}
			
			if (!File.Exists(serialPortPath)) {
				Console.Out.WriteLine("Fatal: Serial port {0} doesn't exist!", serialPortPath);
				mre.Set();
				return;
			}
			
			// ---- Interfacing with the car
			
			Car = new SerialCarHardwareInterface(serialPortPath);
			
			if (shouldStartHTTPServer) {
				Server = new CarHTTPServer(Car, httpPort);
				Console.Out.WriteLine("Started HTTP server on port {0}.", httpPort);
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
			
			if (shouldPrintAccelerometerChanges) {
				Car.Accelerometor.ReadingChanged += delegate(Sensor sender, ReadingChangedEventArgs e) {
					Console.Out.WriteLine("Accelerometer changed to: {0}.", sender.DisplayReading);
				};
			}

			// Loop
			Loop = new CarEventLoop(Car);
			Loop.AddAIHandler(new AIHandler());
			Loop.AddInterruptHandler(new InterruptHandler());
			Loop.StartLoop();

			Thread.Sleep(TimeSpan.FromSeconds(2));
			Loop.StopLoop();

			Thread.Sleep(TimeSpan.FromSeconds(2));
			Loop.StartLoop();

			Thread.Sleep(TimeSpan.FromSeconds(2));
			Loop.StopLoop();


		}
	}
}

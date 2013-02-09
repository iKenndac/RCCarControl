using System;
using System.Threading;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using RCCarCore;

namespace RCCarService {
	class MainClass {

		const string kSettingsFileName = "RCCarService.settings";
		
		static ManualResetEvent mre = new ManualResetEvent(false);
		static string[] applicationArguments;
		
		public static void Main (string[] args) {
			
			applicationArguments = args;
			// Start a thread or two to do some work...
			ThreadPool.QueueUserWorkItem(o => BackgroundWork());
			// Block until our ManualResetEvent is set
			mre.WaitOne();

			if (Display != null)
				Display.ClearScreen();
		}

		static SerialCarHardwareInterface Car { get; set; }
		static CarHTTPServer Server { get; set; }
		static CarEventLoop Loop { get; set; }
		static I2CUIDevice Display { get; set; }
		static MenuController MainMenuController { get; set; }

		static Dictionary<string, string> GetSettings() {

			Dictionary<string, string> settings = new Dictionary<string, string>();

			string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			path = Path.Combine(path, kSettingsFileName);

			if (!File.Exists(path)) 
				return settings;

			using (StreamReader reader = new StreamReader(path)) {
				while (!reader.EndOfStream) {
					string line = reader.ReadLine();

					if (line.Trim().StartsWith("#") || line.Trim().Length == 0)
						continue;

					string[] components = line.Split('=');
					if (components.Length > 1) {
						settings.Add(components[0].Trim(), components[1].Trim());
					}
				}
			}

			return settings;
		}

		static void BackgroundWork() {

			GetSettings();

			bool shouldStartHTTPServer = false;
			bool shouldPrintDistanceChanges = false;
			bool shouldPrintAccelerometerChanges = false;
			int httpPort = 8080;
			string serialPortPath = null;
			string i2cUIDevicePath = null;

			// ---- Settings file

			Dictionary<string, string> settings = GetSettings();
			if (settings.ContainsKey("logdistance"))
				shouldPrintDistanceChanges = (settings["logdistance"] == "1");
			
			if (settings.ContainsKey("logaccel"))
				shouldPrintAccelerometerChanges = (settings["logaccel"] == "1");

			if (settings.ContainsKey("httpport")) {
				httpPort = Convert.ToInt32(settings["httpport"]);
				shouldStartHTTPServer = true;
			}

			if (settings.ContainsKey("serialport"))
				serialPortPath = settings["serialport"];

			if (settings.ContainsKey("i2c_ui"))
				i2cUIDevicePath = settings["i2c_ui"];


			// ---- Command line argument parsing (these override settings)
			
			for (int argIndex = 0; argIndex < applicationArguments.Length; argIndex++) {
				
				string arg = applicationArguments[argIndex];

				if (arg == "-logdistance")
					shouldPrintDistanceChanges = true;
				if (arg == "-logaccel")
					shouldPrintAccelerometerChanges = true;
				
				if (arg == "-httpport") {
					argIndex++;
					try {
						string portString = applicationArguments[argIndex];
						httpPort = Convert.ToInt32(portString);
						shouldStartHTTPServer = true;
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
					} catch {
					}
				}

				if (arg == "-i2c_ui") {
					argIndex++;
					try {
						i2cUIDevicePath = applicationArguments[argIndex];
					} catch {
					}
				}
				
			}

			if (serialPortPath == null) {
				Console.Out.WriteLine("Warning: No serial port given. Set with -serialport.");
			} else {
				if (!File.Exists(serialPortPath)) {
					Console.Out.WriteLine("Warning: Serial port {0} doesn't exist!", serialPortPath);
				}
			}

			if (i2cUIDevicePath == null) {
				Console.Out.WriteLine("Warning: No UI device path given. Set with -i2c_ui.");
			} else {
				if (!File.Exists(i2cUIDevicePath)) {
					Console.Out.WriteLine("Warning: UI device {0} doesn't exist!", i2cUIDevicePath);
				}
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

			// Display

			if (i2cUIDevicePath != null && i2cUIDevicePath.Length > 0) {
				Display = new I2CUIDevice(i2cUIDevicePath, 0x94);

				MenuItem rootMenu = new MenuItem();
				MenuItem sensorMenu = new MenuItem("Sensors & Servos");
				rootMenu.AddChild(sensorMenu);

				MenuItem accelerometerMenuItem = new MenuItem("Accelerometer");
				accelerometerMenuItem.MenuItemChosen += delegate(MenuItem sender, EventArgs e) {
					MainMenuController.PresentInfoScreen(new AccelerometerInfoScreen(Car.Accelerometor));
				};
				sensorMenu.AddChild(accelerometerMenuItem);

				MenuItem distanceMenuItem = new MenuItem("Distances");
				distanceMenuItem.MenuItemChosen += delegate(MenuItem sender, EventArgs e) {
					MainMenuController.PresentInfoScreen(new DistancesInfoScreen(Car.FrontUltrasonicSensors, Car.RearUltrasonicSensor));
				};
				sensorMenu.AddChild(distanceMenuItem);

				MenuItem steeringMenuItem = new MenuItem("Steering Servo");
				steeringMenuItem.MenuItemChosen += delegate(MenuItem sender, EventArgs e) {
					MainMenuController.PresentInfoScreen(new ServoInfoScreen(Car.SteeringServo, "Steering"));
				};
				sensorMenu.AddChild(steeringMenuItem);

				MenuItem throttleMenuItem = new MenuItem("Throttle Servo");
				throttleMenuItem.MenuItemChosen += delegate(MenuItem sender, EventArgs e) {
					MainMenuController.PresentInfoScreen(new ServoInfoScreen(Car.ThrottleServo, "Throttle"));
				};
				sensorMenu.AddChild(throttleMenuItem);

				// --

				MenuItem exitMenuItem = new MenuItem("Shut down");
				exitMenuItem.MenuItemChosen += delegate(MenuItem sender, EventArgs e) {
					ConfirmationPromptInfoScreen exitConfirmation = new ConfirmationPromptInfoScreen("Are you sure?");
					exitConfirmation.RespondToPrompt += delegate(ConfirmationPromptInfoScreen prompt, bool confirm) {
						if (confirm) {
							Display.ClearScreen();
							Display.WriteString("Goodbye!", 0, 4);

							System.Diagnostics.Process proc = new System.Diagnostics.Process();
							proc.EnableRaisingEvents=false; 
							proc.StartInfo.FileName = "shutdown";
							proc.StartInfo.Arguments = "-h now";
							proc.Start();
							proc.WaitForExit();

							mre.Set();
							return;
						}
					};

					MainMenuController.PresentInfoScreen(exitConfirmation);
				};

				rootMenu.AddChild(exitMenuItem);

				MainMenuController = new MenuController(Display, rootMenu);


			}
		}
	}
}

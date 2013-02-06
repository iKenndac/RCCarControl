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
		static I2CUIDevice Display { get; set; }

		static void BackgroundWork() {

			bool shouldStartHTTPServer = false;
			bool shouldPrintDistanceChanges = false;
			bool shouldPrintAccelerometerChanges = false;
			int httpPort = 8080;
			string serialPortPath = null;
			string i2cUIDevicePath = null;
			
			// ---- Command line argument parsing
			
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
					Console.Out.WriteLine("Fatal: Serial port {0} doesn't exist!", serialPortPath);
					mre.Set();
					return;
				}
			}

			if (i2cUIDevicePath == null) {
				Console.Out.WriteLine("Warning: No UI device path given. Set with -i2c_ui.");
			} else {
				if (!File.Exists(i2cUIDevicePath)) {
					Console.Out.WriteLine("Fatal: UI device {0} doesn't exist!", i2cUIDevicePath);
					mre.Set();
					return;
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

			if (i2cUIDevicePath != null) {
				Display = new I2CUIDevice(i2cUIDevicePath, 0x94);
				Display.ButtonsPushed += delegate(I2CUIDevice sender, I2CUIDevice.ButtonMask buttons) {

					string buttonsPushed = "";

					if ((buttons & I2CUIDevice.ButtonMask.Button1) == I2CUIDevice.ButtonMask.Button1)
						buttonsPushed += "1 ";
					
					if ((buttons & I2CUIDevice.ButtonMask.Button2) == I2CUIDevice.ButtonMask.Button2)
						buttonsPushed += "2 ";
					
					if ((buttons & I2CUIDevice.ButtonMask.Button3) == I2CUIDevice.ButtonMask.Button3)
						buttonsPushed += "3 ";
					
					if ((buttons & I2CUIDevice.ButtonMask.Button4) == I2CUIDevice.ButtonMask.Button4)
						buttonsPushed += "4 ";
					
					if ((buttons & I2CUIDevice.ButtonMask.Button5) == I2CUIDevice.ButtonMask.Button5)
						buttonsPushed += "5 ";
					
					if ((buttons & I2CUIDevice.ButtonMask.Button6) == I2CUIDevice.ButtonMask.Button6)
						buttonsPushed += "6 ";

					Display.ClearScreen();
					Display.WriteString("Buttons pushed:", 0, 0);
					Display.WriteString(buttonsPushed, 1, 0);
				};

				string menuButtons = (char)I2CUIDevice.CustomCharacter.Tick + "  " +
					(char)I2CUIDevice.CustomCharacter.Cross + "  " +
						(char)I2CUIDevice.CustomCharacter.Left + "  " +
						(char)I2CUIDevice.CustomCharacter.Right + "  " +
						(char)I2CUIDevice.CustomCharacter.Up + "  " +
						(char)I2CUIDevice.CustomCharacter.Down;

				Display.WriteString(menuButtons, 1, 0);

			}
		}
	}
}

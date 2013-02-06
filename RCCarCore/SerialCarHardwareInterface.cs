using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;

namespace RCCarCore
{
	/// <summary>
	/// This interface defines the juicy stuff for the RC car hardware
	/// interface, just in case we want to have multiple implementations.
	/// </summary>
	public interface ICarHardwareInterface {
		UltrasonicSensor[] FrontUltrasonicSensors { get; }
		UltrasonicSensor RearUltrasonicSensor { get; }
		AccelerometorSensor Accelerometor { get; }
		Servo ThrottleServo { get; }
		Servo SteeringServo { get; }

		CarState CreateState();
		bool ApplyValueToServo(double value, Servo servo);
	}

	public enum UltrasonicSensorIndex : int {
		FrontLeft = 0,
		FrontMiddle = 1,
		FrontRight = 2
	}
	
	/// <summary>
	/// RC car hardware interface. This class is responsible for talking to
	/// the sensor module (i.e., an Arduino with stuff attached) and managing
	/// objects representing the various sensors and servos attached to it.
	/// </summary>
	public class SerialCarHardwareInterface : ICarHardwareInterface, IDisposable {

		public SerialCarHardwareInterface (String portPath)
		{
			// For now, we're hardcoding our sensors.
			RearUltrasonicSensor = new UltrasonicSensor();
			FrontUltrasonicSensors = new UltrasonicSensor[] {
				new UltrasonicSensor(),
				new UltrasonicSensor(),
				new UltrasonicSensor()
			};
			Accelerometor = new AccelerometorSensor();
			ThrottleServo = new Servo(this);
			SteeringServo = new Servo(this);

			SerialPortWorker = new BackgroundWorker();
			SerialPortWorker.WorkerReportsProgress = true;
			SerialPortWorker.WorkerSupportsCancellation = true;
			SerialPortWorker.DoWork += RunSerialPortThread;
			SerialPortWorker.ProgressChanged += delegate(object sender, ProgressChangedEventArgs e) {
				string line = (string)e.UserState;
				HandleLineFromSerialPort(line);
			};
			SerialPortWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e) {
				if (e.Error != null)
					Console.Out.WriteLine("Communication thread failed with: {0}: {1}", e.Error.GetType().FullName, e.Error.Message);
				else
					Console.Out.WriteLine("Communication thread finished normally.");
			};
			SerialPortWorker.RunWorkerAsync(portPath);

		}

		public void Dispose() {
			SerialPortWorker.CancelAsync();
			while (SerialPortWorker.IsBusy)
				Thread.Sleep(TimeSpan.FromMilliseconds(10));
		}

		public UltrasonicSensor[] FrontUltrasonicSensors { get; private set; }
		public UltrasonicSensor RearUltrasonicSensor { get; private set; }
		public AccelerometorSensor Accelerometor { get; private set; }
		public Servo ThrottleServo { get; private set; }
		public Servo SteeringServo { get; private set; }
		private BackgroundWorker SerialPortWorker { get; set; }


		/// <summary>
		/// Creates an immutable copy of the interface's current state.
		/// </summary>
		/// <returns>
		/// The state.
		/// </returns>
		public CarState CreateState() {
			CarState state = new CarState();

			state.Accelerometer = new AccelerometorSensor();
			state.Accelerometer.SetValues(Accelerometor.X, Accelerometor.Y, Accelerometor.Z);

			state.RearUltrasonicSensor = new UltrasonicSensor();
			state.RearUltrasonicSensor.DistanceReadingCM = RearUltrasonicSensor.DistanceReadingCM;

			state.FrontUltrasonicSensors = new UltrasonicSensor[] {
				new UltrasonicSensor(),
				new UltrasonicSensor(),
				new UltrasonicSensor()
			};

			state.FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontLeft].DistanceReadingCM = FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontLeft].DistanceReadingCM;
			state.FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontMiddle].DistanceReadingCM = FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontMiddle].DistanceReadingCM;
			state.FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontRight].DistanceReadingCM = FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontRight].DistanceReadingCM;

			state.SteeringServo = new Servo(null);
			state.SteeringServo.Value = SteeringServo.Value;

			state.ThrottleServo = new Servo(null);
			state.ThrottleServo.Value = ThrottleServo.Value;

			return state;
		}

		public bool ApplyValueToServo(double value, Servo servo) {

			double throttleValue = ThrottleServo.Value;
			double steeringValue = SteeringServo.Value;

			if (servo == ThrottleServo) throttleValue = value;
			if (servo == SteeringServo) steeringValue = value;

			return WriteServoValuesToDevice(steeringValue, throttleValue);
		}

		private bool WriteServoValuesToDevice(double steering, double throttle) {
			// Serial protocol expects servo values in integral degrees from 
			// 0->180, while our objects have floating point -1.0->1.0.
			byte steeringValue = (byte)((steering * 90.0) + 90);
			byte throttleValue = (byte)((throttle * 90.0) + 90); 
			
			byte[] message = new byte[5];
			message[0] = 0xBA;
			message[1] = 0xBE;
			message[2] = steeringValue;
			message[3] = throttleValue;
			message[4] = 0;
			
			// Checksum is the XOR of the message content.
			for (int index = 2; index <= 3; index++)
				message[4] ^= message[index];

			// Since this isn't executed on the communication thread,
			// we need to lock the message queue to add the message.
			lock(messageQueue) {
				messageQueue.Add(message);
			}
			
			return true;
		}

		private void HandleLineFromSerialPort(string line) {
			
			if (line.StartsWith("DISTANCE:")) {
				
				string distanceString = line.Remove(0, "DISTANCE:".Length).Trim();
				string[] distanceStrings = distanceString.Split(',');
				List<int> distances = new List<int>();
				foreach (string distanceStringRepresentation in distanceStrings) {
					try {
						distances.Add(Convert.ToInt32(distanceStringRepresentation));
					} catch {
						distances.Add(0);
					}
				}

				HandleDistanceUpdate(distances.ToArray());
				return;
			}

			if (line.StartsWith("SERVO:")) {
				string servoMessage = line.Remove(0, "SERVO:".Length).Trim();
				HandleServoResponse(servoMessage);
				return;
			}

			if (line.StartsWith("ACCEL:")) {

				string accelString = line.Remove(0, "ACCEL:".Length).Trim();
				string[] accelStrings = accelString.Split(',');
				List<double> accels = new List<double>();
				foreach (string accelStringRepresentation in accelStrings) {
					try {
						accels.Add(Convert.ToDouble(accelStringRepresentation));
					} catch {
						accels.Add(0.00);
					}
				}
				
				HandleAccelerationUpdate(accels.ToArray());
				return;
			}
		}

		void HandleAccelerationUpdate(double[] accels) {

			if (accels.Length != 3) {
				Console.Out.WriteLine("Got unexpected number of acceleration values: {0}", accels.Length);
				return;
			}

			double x = accels[0];
			double y = accels[1];
			double z = accels[2];
			Accelerometor.SetValues(x, y, z);
		}

		void HandleServoResponse(string servoMessage) {
			if (servoMessage != "OK")
				Console.Out.WriteLine("WARNING: Got servo response: {0}", servoMessage);
		}
		
		private const int kRearDistanceSensorIndex = 3;
		private const int kFrontLeftDistanceSensorIndex = 1;
		private const int kFrontMiddleDistanceSensorIndex = 0;
		private const int kFrontRightDistanceSensorIndex = 2;
		
		private void HandleDistanceUpdate(int[] distances) {
			
			if (distances.Length != 4) {
				Console.Out.WriteLine("Got unexpected number of distances: {0}", distances.Length);
				return;
			}
			
			// Todo: Find a better way of defining which sensor is where.
			// This should probably be an implementation detail of the 
			// Arduino sketch.
			int rearDistanceValue = distances[kRearDistanceSensorIndex];
			int frontLeftDistanceValue = distances[kFrontLeftDistanceSensorIndex];
			int frontMiddleDistanceValue = distances[kFrontMiddleDistanceSensorIndex];
			int frontRightDistanceValue = distances[kFrontRightDistanceSensorIndex];

			RearUltrasonicSensor.DistanceReadingCM = rearDistanceValue;
			FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontLeft].DistanceReadingCM = frontLeftDistanceValue;
			FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontMiddle].DistanceReadingCM = frontMiddleDistanceValue;
			FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontRight].DistanceReadingCM = frontRightDistanceValue;
		}

		#region Serial Port Thread

		private List<byte[]> messageQueue = new List<byte[]>();
		private List<char> buffer = new List<char>();

		private void RunSerialPortThread(object sender, DoWorkEventArgs e) {

			BackgroundWorker worker = (BackgroundWorker)sender;

			string portName = (string)e.Argument;
			SerialPort port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
			port.Open();

			Console.Out.WriteLine("Connected to car on {0}.", portName);

			// Opening the serial port causes an Arduino reset, so we
			// need to wait a bit before allowing commands to be sent.
			Thread.Sleep(2000);

			while (!worker.CancellationPending) {

				lock (messageQueue) {
					if (messageQueue.Count > 0) {
						foreach (byte[] message in messageQueue)
							port.Write(message, 0, message.Length);
					}
					messageQueue.Clear();
				}

				char newByte = (char)port.ReadByte();
				buffer.Add(newByte);
				if (newByte == 10) {
					string line = new string(buffer.ToArray(), 0, buffer.Count);
					line = line.Trim();
					buffer.Clear();
					worker.ReportProgress(0, line);
				}
			}

			port.Close();
			port.Dispose();
			port = null;

			if (worker.CancellationPending) e.Cancel = true;

		}

		#endregion

	}
}

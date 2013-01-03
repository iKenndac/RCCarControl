using System;
using System.IO.Ports;

namespace RCCarControl
{
	/// <summary>
	/// This interface defines the juicy stuff for the RC car hardware
	/// interface, just in case we want to have multiple implementations.
	/// </summary>
	public interface IRCCarHardwareInterface {
		UltrasonicSensor[] FrontUltrasonicSensors { get; }
		UltrasonicSensor[] RearUltrasonicSensors { get; }
		AccelerometorSensor Accelerometor { get; }
		Servo ThrottleServo { get; }
		Servo SteeringServo { get; }

		bool ApplyValueToServo(double value, Servo servo);
	}

	/// <summary>
	/// RC car hardware interface. This class is responsible for talking to
	/// the sensor module (i.e., an Arduino with stuff attached) and managing
	/// objects representing the various sensors and servos attached to it.
	/// </summary>
	public class SerialRCCarHardwareInterface : IRCCarHardwareInterface {
		public SerialRCCarHardwareInterface(String portPath) {
			// For now, we're hardcoding our sensors.
			RearUltrasonicSensors = new UltrasonicSensor[] { new UltrasonicSensor() };
			FrontUltrasonicSensors = new UltrasonicSensor[] { new UltrasonicSensor(), new UltrasonicSensor(), new UltrasonicSensor() };
			Accelerometor = new AccelerometorSensor();
			ThrottleServo = new Servo(this);
			SteeringServo = new Servo(this);
		}

		public UltrasonicSensor[] FrontUltrasonicSensors { get; private set; }
		public UltrasonicSensor[] RearUltrasonicSensors { get; private set; }
		public AccelerometorSensor Accelerometor { get; private set; }
		public Servo ThrottleServo { get; private set; }
		public Servo SteeringServo { get; private set; }
		private SerialPort Port { get; set; }
		
		public bool ApplyValueToServo(double value, Servo servo) {

			double throttleValue = ThrottleServo.Value;
			double steeringValue = SteeringServo.Value;

			if (servo == ThrottleServo) throttleValue = value;
			if (servo == SteeringServo) steeringValue = value;

			return WriteServoValuesToDevice(steeringValue, throttleValue);
		}


		private bool WriteServoValuesToDevice(double steering, double throttle) {

			if (!Port.IsOpen) {
				Console.WriteLine("Write failed: Port not open.");
				return false;
			}

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

			try {
				Port.Write(message, 0, message.Length);

				String response = Port.ReadLine();
				if (response.Trim().Equals("OK"))
					return true;

				throw new Exception(response);

			} catch (Exception e) {
				Console.WriteLine("Write failed: " + e.Message);
				return false;
			}
		}

	}
}


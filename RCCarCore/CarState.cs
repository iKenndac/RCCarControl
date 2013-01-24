using System;

namespace RCCarCore {
	public class CarState {
		public CarState() {
		}

		public Servo SteeringServo { get; set; }
		public Servo ThrottleServo { get; set; }

		public AccelerometorSensor Accelerometer { get; set; }
		public UltrasonicSensor RearUltrasonicSensor { get; set; }
		public UltrasonicSensor[] FrontUltrasonicSensors { get; set; }



	}
}


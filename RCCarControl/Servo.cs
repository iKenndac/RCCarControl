using System;

namespace RCCarControl {

	public class Servo {

		private double _value;
		private ICarHardwareInterface _hardwareInterface;

		internal Servo(ICarHardwareInterface hardwareInterface) {
			_hardwareInterface = hardwareInterface;
			_value = 0.0;
		}

		public double Value {
			get { return _value; }
			set {
				if (_hardwareInterface == null || _hardwareInterface.ApplyValueToServo(value, this)) {
					// ^ Allow setting the value when there's no hardware interface.
					_value = value;
				}
			}
		}
	}
	
}


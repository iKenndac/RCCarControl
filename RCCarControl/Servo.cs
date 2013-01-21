using System;

namespace RCCarControl {

	public class Servo {

		private double _value;
		private IRCCarHardwareInterface _hardwareInterface;

		internal Servo(IRCCarHardwareInterface hardwareInterface) {
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


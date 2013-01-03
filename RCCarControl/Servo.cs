using System;

namespace RCCarControl {

	public class Servo {

		private double _value;
		private IRCCarHardwareInterface _hardwareInterface;

		internal Servo(IRCCarHardwareInterface hardwareInterface) {
			_hardwareInterface = hardwareInterface;
			Value = 0.0;
		}

		public double Value {
			get { return _value; }
			set {
				if (_hardwareInterface.ApplyValueToServo(value, this)) {
					_value = value;
				}
			}
		}
	}
	
}


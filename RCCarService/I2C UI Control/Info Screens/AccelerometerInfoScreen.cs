using System;
using System.Text;
using RCCarCore;

namespace RCCarService {
	public class AccelerometerInfoScreen : InfoScreen {

		public AccelerometerInfoScreen(AccelerometorSensor accel) {
			Accelerometer = accel;
		}

		public AccelerometorSensor Accelerometer { get; private set; }
		
		public override void Activate(I2CUIDevice screen) {
			base.Activate(screen);
			Device.ClearScreen();
			Device.WriteString(Encoding.ASCII.GetString(new byte[] { (byte)I2CUIDevice.CustomCharacter.Left }), 1, 0);
			UpdateScreen();
			Accelerometer.ReadingChanged += AccelerometerUpdated;
		}

		public override void Deactivate() {
			base.Deactivate();
			Accelerometer.ReadingChanged -= AccelerometerUpdated;
		}

		private void AccelerometerUpdated(Sensor sender, ReadingChangedEventArgs e) {
			UpdateScreen();
		}

		private void UpdateScreen() {
			Device.WriteString(String.Format("X:{0:+0.00;-0.00} Y:{1:+0.00;-0.00}", Accelerometer.X, Accelerometer.Y), 0, 0);
			Device.WriteString(String.Format("Z:{0:+0.00;-0.00}", Accelerometer.Z), 1, 8);
		}
		
		internal override void HandleButtons(I2CUIDevice sender, I2CUIDevice.ButtonMask buttons) {
			if ((buttons & I2CUIDevice.ButtonMask.Button1) == I2CUIDevice.ButtonMask.Button1)
				HandleBackButton();
		}
		
		private void HandleBackButton() {
			NotifyExit();
		}

	}
}


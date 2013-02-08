using System;
using System.Text;
using System.Timers;
using RCCarCore;

namespace RCCarService {
	public class AccelerometerInfoScreen : InfoScreen {

		public AccelerometerInfoScreen(AccelerometorSensor accel) {
			Accelerometer = accel;
		}

		public AccelerometorSensor Accelerometer { get; private set; }
		private Timer RefreshTimer { get; set; }
		
		public override void Activate(I2CUIDevice screen) {
			base.Activate(screen);
			Device.ClearScreen();
			Device.WriteButtonSymbol(I2CUIDevice.CustomCharacter.Left, I2CUIDevice.ButtonSymbolPosition.Button1);
			UpdateScreen();
			RefreshTimer = new Timer(100);
			RefreshTimer.AutoReset = true;
			RefreshTimer.Elapsed += delegate(object sender, ElapsedEventArgs e) {
				UpdateScreen();
			};
			RefreshTimer.Enabled = true;
		}

		public override void Deactivate() {
			base.Deactivate();
			RefreshTimer.Enabled = false;
			RefreshTimer = null;
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


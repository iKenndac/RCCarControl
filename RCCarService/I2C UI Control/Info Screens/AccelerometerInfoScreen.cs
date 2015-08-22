using System;
using System.Threading;
using RCCarCore;

namespace RCCarService {
    
    internal class AccelerometerInfoScreen : InfoScreen {

		public AccelerometerInfoScreen(AccelerometorSensor accel) {
			Accelerometer = accel;
		}

		public AccelerometorSensor Accelerometer { get; private set; }
		private Timer RefreshTimer { get; set; }
		
		internal override void Activate(I2CUIDevice screen) {
			base.Activate(screen);
			Device.ClearScreen();
			Device.WriteButtonSymbol(CustomCharacter.Left, ButtonSymbolPosition.Button1);
			UpdateScreen();
            TimerCallback callback = delegate (object sender)
            {
                UpdateScreen();
            };
            RefreshTimer = new Timer(callback, null, 0, 250);
		}

		internal override void Deactivate() {
			base.Deactivate();
            RefreshTimer.Dispose();
			RefreshTimer = null;
		}

		private void UpdateScreen() {
			Device.WriteString(String.Format("X:{0:+0.00;-0.00} Y:{1:+0.00;-0.00}", Accelerometer.X, Accelerometer.Y), 0, 0);
			Device.WriteString(String.Format("Z:{0:+0.00;-0.00}", Accelerometer.Z), 1, 8);
		}
		
		internal override void HandleButtons(I2CUIDevice sender, ButtonMask buttons) {
			if ((buttons & ButtonMask.Button1) == ButtonMask.Button1)
				HandleBackButton();
		}
		
		private void HandleBackButton() {
			NotifyExit();
		}

	}
}


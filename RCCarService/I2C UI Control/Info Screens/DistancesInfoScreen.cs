using System;
using System.Timers;
using RCCarCore;

namespace RCCarService {
	public class DistancesInfoScreen : InfoScreen {

		public DistancesInfoScreen(UltrasonicSensor[] frontSensors, UltrasonicSensor rearSensor) {
			FrontSensors = frontSensors;
			RearSensor = rearSensor;
		}

		public UltrasonicSensor[] FrontSensors { get; private set; }
		public UltrasonicSensor RearSensor { get; private set; }
		private Timer RefreshTimer { get; set; }
		
		public override void Activate(I2CUIDevice screen) {
			base.Activate(screen);
			Device.ClearScreen();
			Device.WriteButtonSymbol(I2CUIDevice.CustomCharacter.Left, I2CUIDevice.ButtonSymbolPosition.Button1);
			UpdateScreen();
			RefreshTimer = new Timer(500);
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
			Device.WriteString(String.Format("FL:{0:000} FM:{1:000}", FrontSensors[(int)UltrasonicSensorIndex.FrontLeft].DistanceReadingCM, FrontSensors[(int)UltrasonicSensorIndex.FrontMiddle].DistanceReadingCM), 0, 2);
			Device.WriteString(String.Format("FR:{0:000} RM:{1:000}", FrontSensors[(int)UltrasonicSensorIndex.FrontRight].DistanceReadingCM, RearSensor.DistanceReadingCM), 1, 2);
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


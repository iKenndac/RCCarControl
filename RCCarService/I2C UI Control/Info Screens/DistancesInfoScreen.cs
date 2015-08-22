using System;
using System.Threading;
using System.Runtime.InteropServices.WindowsRuntime;
using RCCarCore;

namespace RCCarService {

	internal class DistancesInfoScreen : InfoScreen {

		public DistancesInfoScreen([ReadOnlyArray()] UltrasonicSensor[] frontSensors, UltrasonicSensor rearSensor) {
			FrontSensors = (UltrasonicSensor[])frontSensors.Clone();
			RearSensor = rearSensor;
		}

		public UltrasonicSensor[] FrontSensors { get; private set; }
		public UltrasonicSensor RearSensor { get; private set; }
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
            RefreshTimer = new Timer(callback, null, 0, 500);
        }
		
		internal override void Deactivate() {
			base.Deactivate();
			RefreshTimer.Dispose();
			RefreshTimer = null;
		}
		
		private void UpdateScreen() {
			Device.WriteString(String.Format("FL:{0:000} FM:{1:000}", FrontSensors[(int)UltrasonicSensorIndex.FrontLeft].DistanceReadingCM, FrontSensors[(int)UltrasonicSensorIndex.FrontMiddle].DistanceReadingCM), 0, 2);
			Device.WriteString(String.Format("FR:{0:000} RM:{1:000}", FrontSensors[(int)UltrasonicSensorIndex.FrontRight].DistanceReadingCM, RearSensor.DistanceReadingCM), 1, 2);
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


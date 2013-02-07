using System;
using System.Text;
using RCCarCore;

namespace RCCarService {
	public class ServoInfoScreen : InfoScreen {

		public ServoInfoScreen(Servo servo, string name) {
			ScreenServo = servo;
			Name = name;
		}

		public Servo ScreenServo { get; private set; }
		public string Name { get; private set; }

		public override void Activate(I2CUIDevice screen) {
			base.Activate(screen);
			Device.ClearScreen();
			Device.WriteButtonSymbol(I2CUIDevice.CustomCharacter.Left, I2CUIDevice.ButtonSymbolPosition.Button1);
			Device.WriteButtonSymbol(I2CUIDevice.CustomCharacter.Up, I2CUIDevice.ButtonSymbolPosition.Button3);
			Device.WriteButtonSymbol(I2CUIDevice.CustomCharacter.Down, I2CUIDevice.ButtonSymbolPosition.Button4);
			UpdateScreen();
		}

		private void UpdateScreen() {
			Device.WriteString(String.Format("{0}: {1:0.00}    ", Name, ScreenServo.Value), 0, 0);
		}

		internal override void HandleButtons(I2CUIDevice sender, I2CUIDevice.ButtonMask buttons) {

			if ((buttons & I2CUIDevice.ButtonMask.Button1) == I2CUIDevice.ButtonMask.Button1)
				HandleBackButton();
			else if ((buttons & I2CUIDevice.ButtonMask.Button3) == I2CUIDevice.ButtonMask.Button3)
				HandleUpButton();
			else if ((buttons & I2CUIDevice.ButtonMask.Button4) == I2CUIDevice.ButtonMask.Button4)
				HandleDownButton();
		}

		private void HandleBackButton() {
			NotifyExit();
		}

		private void HandleUpButton() {

			double newValue = ScreenServo.Value;
			newValue += 0.1;
			newValue = Math.Min(newValue, 1.0);
			ScreenServo.Value = newValue;
			
			UpdateScreen();
		}
		
		private void HandleDownButton() {
			double newValue = ScreenServo.Value;
			newValue -= 0.1;
			newValue = Math.Max(newValue, -1.0);
			ScreenServo.Value = newValue;
			
			UpdateScreen();
		}
	}
}


using System;
using System.Text;

namespace RCCarService {

     public sealed class Servo
     {
        public double Value
        {
            get; set;
        }
        // This is a stub until RCCarCore is ported.
    }

	internal class ServoInfoScreen : InfoScreen {

		public ServoInfoScreen(Servo servo, string name) {
			ScreenServo = servo;
			Name = name;
		}

		public Servo ScreenServo { get; private set; }
		public string Name { get; private set; }

		internal override void Activate(I2CUIDevice screen) {
			base.Activate(screen);
			Device.ClearScreen();
			Device.WriteButtonSymbol(CustomCharacter.Left, ButtonSymbolPosition.Button1);
			Device.WriteButtonSymbol(CustomCharacter.Up, ButtonSymbolPosition.Button3);
			Device.WriteButtonSymbol(CustomCharacter.Down, ButtonSymbolPosition.Button4);
			UpdateScreen();
		}

		private void UpdateScreen() {
			Device.WriteString(String.Format("{0}: {1:0.00}    ", Name, ScreenServo.Value), 0, 0);
		}

		internal override void HandleButtons(I2CUIDevice sender, ButtonMask buttons) {

			if ((buttons & ButtonMask.Button1) == ButtonMask.Button1)
				HandleBackButton();
			else if ((buttons & ButtonMask.Button3) == ButtonMask.Button3)
				HandleUpButton();
			else if ((buttons & ButtonMask.Button4) == ButtonMask.Button4)
				HandleDownButton();
		}

		private void HandleBackButton() {
			NotifyExit();
		}

		private void HandleUpButton() {

			double newValue = ScreenServo.Value;
			newValue += 0.05;
			newValue = Math.Min(newValue, 1.0);
			ScreenServo.Value = newValue;
			
			UpdateScreen();
		}
		
		private void HandleDownButton() {
			double newValue = ScreenServo.Value;
			newValue -= 0.05;
			newValue = Math.Max(newValue, -1.0);
			ScreenServo.Value = newValue;
			
			UpdateScreen();
		}
	}
}


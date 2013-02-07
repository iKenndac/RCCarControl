using System;

namespace RCCarService {
	public class ConfirmationPromptInfoScreen : InfoScreen {

		public ConfirmationPromptInfoScreen(string prompt) {
			Prompt = prompt;
		}

		public delegate void RespondToPromptEventHandler(ConfirmationPromptInfoScreen sender, bool confirm);
		public event RespondToPromptEventHandler RespondToPrompt;

		public string Prompt { get; private set; }

		public override void Activate(I2CUIDevice uiDevice) {
			base.Activate(uiDevice);

			Device.ClearScreen();
			Device.WriteString(Prompt, 0, 0);
			Device.WriteButtonSymbol(I2CUIDevice.CustomCharacter.Cross, I2CUIDevice.ButtonSymbolPosition.Button2);
			Device.WriteButtonSymbol(I2CUIDevice.CustomCharacter.Tick, I2CUIDevice.ButtonSymbolPosition.Button5);
		}

		internal override void HandleButtons(I2CUIDevice sender, I2CUIDevice.ButtonMask buttons) {

			bool confirm = false;

			if ((buttons & I2CUIDevice.ButtonMask.Button2) == I2CUIDevice.ButtonMask.Button2)
				confirm = false;
			else if ((buttons & I2CUIDevice.ButtonMask.Button5) == I2CUIDevice.ButtonMask.Button5)
				confirm = true;
			else
				return;

			if (RespondToPrompt != null)
				RespondToPrompt(this, confirm);

			NotifyExit();
		}

	}
}


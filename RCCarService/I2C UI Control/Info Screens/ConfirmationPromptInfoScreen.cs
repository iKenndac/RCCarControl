using System;

namespace RCCarService {

    internal delegate void RespondToPromptEventHandler(ConfirmationPromptInfoScreen sender, bool confirm);

    enum ConfirmationPromptButtons
    {
        Tick,
        Cross,
        TickAndCross
    }

    internal class ConfirmationPromptInfoScreen : InfoScreen {

		public ConfirmationPromptInfoScreen(string prompt) : this(prompt, ConfirmationPromptButtons.TickAndCross) {
		}

        public ConfirmationPromptInfoScreen(string prompt, ConfirmationPromptButtons buttons)
        {
            Prompt = prompt;
            Buttons = buttons;
        }

		public event RespondToPromptEventHandler RespondToPrompt;

		public string Prompt { get; private set; }
        public ConfirmationPromptButtons Buttons { get; private set; }

        internal override void Activate(I2CUIDevice uiDevice) {
            base.Activate(uiDevice);

            Device.ClearScreen();
            Device.WriteString(Prompt, 0, 0);

            if (Buttons == ConfirmationPromptButtons.Cross || Buttons == ConfirmationPromptButtons.TickAndCross) {  
               Device.WriteButtonSymbol(CustomCharacter.Cross, ButtonSymbolPosition.Button2);
            }

            if (Buttons == ConfirmationPromptButtons.Tick || Buttons == ConfirmationPromptButtons.TickAndCross)
            {
                Device.WriteButtonSymbol(CustomCharacter.Tick, ButtonSymbolPosition.Button5);
            }
		}

		internal override void HandleButtons(I2CUIDevice sender, ButtonMask buttons) {

			bool confirm = false;
            bool tickAllowed = Buttons == ConfirmationPromptButtons.Tick || Buttons == ConfirmationPromptButtons.TickAndCross;
            bool crossAllowed = Buttons == ConfirmationPromptButtons.Cross || Buttons == ConfirmationPromptButtons.TickAndCross;

            if ((buttons & ButtonMask.Button2) == ButtonMask.Button2 && crossAllowed)
            {
                confirm = false;
            }
            else if ((buttons & ButtonMask.Button5) == ButtonMask.Button5 && tickAllowed)
            {
                confirm = true;
            }
            else
            {
                return;
            }

            if (RespondToPrompt != null)
            {
                RespondToPrompt(this, confirm);
            }
			NotifyExit();
		}

	}
}


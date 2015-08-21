using System;

namespace RCCarService {

    internal delegate void ReadyToExitEventHandler(InfoScreen sender);

    internal class InfoScreen {
		public InfoScreen() {
		}

		public I2CUIDevice Device { get; private set; }
		public event ReadyToExitEventHandler ReadyToExit;

		internal virtual void Activate(I2CUIDevice uiDevice) {
			Device = uiDevice;
		}

		internal virtual void Deactivate() {
			Device = null;
		}

		internal virtual void HandleButtons(I2CUIDevice sender, ButtonMask buttons) {
		}

		protected void NotifyExit() {
			if (ReadyToExit != null)
				ReadyToExit(this);
		}

	}
}


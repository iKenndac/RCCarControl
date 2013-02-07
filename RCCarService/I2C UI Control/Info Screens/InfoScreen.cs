using System;

namespace RCCarService {
	public class InfoScreen {
		public InfoScreen() {
		}

		public I2CUIDevice Device { get; private set; }

		public delegate void ReadyToExitEventHandler(InfoScreen sender);
		public event ReadyToExitEventHandler ReadyToExit;

		public virtual void Activate(I2CUIDevice uiDevice) {
			Device = uiDevice;
		}

		public virtual void Deactivate() {
			Device = null;
		}

		internal virtual void HandleButtons(I2CUIDevice sender, I2CUIDevice.ButtonMask buttons) {
		}

		protected void NotifyExit() {
			if (ReadyToExit != null)
				ReadyToExit(this);
		}

	}
}


using System;
using System.Text;

namespace RCCarService {
	public class MenuController {

		public MenuController(I2CUIDevice device, MenuItem rootMenu) {
			Device = device;
			CurrentMenu = rootMenu;
			DisplayedMenuIndex = 0;

			Device.ButtonsPushed += HandleButtons;

			ResetScreenState();
			UpdateScreen();
		}

		private I2CUIDevice Device { get; set; }

		private int DisplayedMenuIndex { get; set; }
		private MenuItem CurrentMenu { get; set; }
		public InfoScreen CurrentInfoScreen { get; private set; }

		private bool couldGoBack;
		private bool didHaveSubmenus;

		private void ResetScreenState() {
			couldGoBack = false;
			didHaveSubmenus = false;

			Device.ClearScreen();
			Device.WriteString(Encoding.ASCII.GetString(new byte[] { (byte)I2CUIDevice.CustomCharacter.Up }), 1, 6);
			Device.WriteString(Encoding.ASCII.GetString(new byte[] { (byte)I2CUIDevice.CustomCharacter.Down }), 1, 9);
			Device.WriteString(Encoding.ASCII.GetString(new byte[] { (byte)I2CUIDevice.CustomCharacter.Tick }), 1, 15);
		}

		public void ResetScreen() {
			if (CurrentInfoScreen != null) return;
			ResetScreenState();
			UpdateScreen();
		}

		public void PresentInfoScreen(InfoScreen screen) {
			CurrentInfoScreen = screen;
			if (CurrentInfoScreen == null) {
				DismissInfoScreen();
				return;
			}

			CurrentInfoScreen.ReadyToExit += InfoScreenExitHandler;
			CurrentInfoScreen.Activate(Device);
		}

		public void DismissInfoScreen() {
			if (CurrentInfoScreen != null) {
				CurrentInfoScreen.ReadyToExit -= InfoScreenExitHandler;
				CurrentInfoScreen.Deactivate();
				CurrentInfoScreen = null;
			}

			ResetScreen();
		}

		private void InfoScreenExitHandler(InfoScreen sender) {
			if (sender == CurrentInfoScreen)
				DismissInfoScreen();
		}

		private void UpdateScreen() {

			MenuItem displayedItem = CurrentMenu.ChildItems[DisplayedMenuIndex];

			string firstLine = displayedItem.Title;

			// Make sure to either truncate or pad out the text for clean drawing.
			if (firstLine.Length > 16) {
				firstLine = firstLine.Substring(0, 15);
				firstLine += (char)I2CUIDevice.CustomCharacter.Ellipsis;
			} else if (firstLine.Length < 16) {
				firstLine = firstLine.PadRight(16);
			}

			Device.WriteString(firstLine, 0, 0);

			bool canGoBack = (CurrentMenu.Parent != null);
			if (canGoBack != couldGoBack) {
				Device.WriteString(canGoBack ? Encoding.ASCII.GetString(new byte[] { (byte)I2CUIDevice.CustomCharacter.Left }) : " ", 1, 0);
				couldGoBack = canGoBack;
			}

			bool hasSubmenus = (displayedItem.ChildItems.Length != 0);
			if (didHaveSubmenus != hasSubmenus) {
				I2CUIDevice.CustomCharacter character = hasSubmenus ? I2CUIDevice.CustomCharacter.Right : I2CUIDevice.CustomCharacter.Tick;
				Device.WriteString(Encoding.ASCII.GetString(new byte[] { (byte)character }), 1, 15);
				didHaveSubmenus = hasSubmenus;
			}

		}

		private void HandleButtons(I2CUIDevice sender, I2CUIDevice.ButtonMask buttons) {

			if (CurrentInfoScreen != null) {
				CurrentInfoScreen.HandleButtons(sender, buttons);
				return;
			}

			if ((buttons & I2CUIDevice.ButtonMask.Button1) == I2CUIDevice.ButtonMask.Button1)
				HandleBackButton();
			else if ((buttons & I2CUIDevice.ButtonMask.Button3) == I2CUIDevice.ButtonMask.Button3)
				HandleUpButton();
			else if ((buttons & I2CUIDevice.ButtonMask.Button4) == I2CUIDevice.ButtonMask.Button4)
				HandleDownButton();
			else if ((buttons & I2CUIDevice.ButtonMask.Button6) == I2CUIDevice.ButtonMask.Button6)
				HandleSelectButton();

		}

		private void HandleBackButton() {
			if (CurrentMenu.Parent == null)
				return;

			MenuItem oldCurrentMenu = CurrentMenu;
			CurrentMenu = oldCurrentMenu.Parent;
			DisplayedMenuIndex = Array.IndexOf(CurrentMenu.ChildItems, oldCurrentMenu);
			UpdateScreen();
		}

		private void HandleSelectButton() {

			MenuItem displayedItem = CurrentMenu.ChildItems[DisplayedMenuIndex];

			if (displayedItem.ChildItems.Length > 0) {
				CurrentMenu = displayedItem;
				DisplayedMenuIndex = 0;
				UpdateScreen();
			} else {
				displayedItem.HandleBeingChosen();
			}
		}

		private void HandleUpButton() {
			if (DisplayedMenuIndex == 0)
				DisplayedMenuIndex = CurrentMenu.ChildItems.Length - 1;
			else
				DisplayedMenuIndex--;

			UpdateScreen();
		}

		private void HandleDownButton() {
			if (DisplayedMenuIndex == (CurrentMenu.ChildItems.Length - 1))
			    DisplayedMenuIndex = 0;
			else
			    DisplayedMenuIndex++;

			UpdateScreen();
		}

	}
}


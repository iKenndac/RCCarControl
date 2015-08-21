using System;
using System.Text;

namespace RCCarService {
	public sealed class MenuController {

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
		internal InfoScreen CurrentInfoScreen { get; private set; }

		private bool couldGoBack;
		private bool didHaveSubmenus;

		private void ResetScreenState() {
			couldGoBack = false;
			didHaveSubmenus = false;

			Device.ClearScreen();
			Device.WriteButtonSymbol(CustomCharacter.Up, ButtonSymbolPosition.Button3);
			Device.WriteButtonSymbol(CustomCharacter.Down, ButtonSymbolPosition.Button4);
			Device.WriteButtonSymbol(CustomCharacter.Tick, ButtonSymbolPosition.Button6);
		}

		public void ResetScreen() {
			if (CurrentInfoScreen != null) return;
			ResetScreenState();
			UpdateScreen();
		}

		internal void PresentInfoScreen(InfoScreen screen) {
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
				firstLine += (char)CustomCharacter.Ellipsis;
			} else if (firstLine.Length < 16) {
				firstLine = firstLine.PadRight(16);
			}

			Device.WriteString(firstLine, 0, 0);

			bool canGoBack = (CurrentMenu.Parent != null);
			if (canGoBack != couldGoBack) {
				if (canGoBack)
					Device.WriteButtonSymbol(CustomCharacter.Left, ButtonSymbolPosition.Button1);
				else
					Device.WriteButtonSymbol(CustomCharacter.Blank, ButtonSymbolPosition.Button1);

				couldGoBack = canGoBack;
			}

			bool hasSubmenus = (displayedItem.ChildItems.Length != 0);
			if (didHaveSubmenus != hasSubmenus) {
				if (hasSubmenus)
					Device.WriteButtonSymbol(CustomCharacter.Right, ButtonSymbolPosition.Button6);
				else
					Device.WriteButtonSymbol(CustomCharacter.Tick, ButtonSymbolPosition.Button6);

				didHaveSubmenus = hasSubmenus;
			}

		}

		private void HandleButtons(I2CUIDevice sender, ButtonMask buttons) {

			if (CurrentInfoScreen != null) {
				CurrentInfoScreen.HandleButtons(sender, buttons);
				return;
			}

			if ((buttons & ButtonMask.Button1) == ButtonMask.Button1)
				HandleBackButton();
			else if ((buttons & ButtonMask.Button3) == ButtonMask.Button3)
				HandleUpButton();
			else if ((buttons & ButtonMask.Button4) == ButtonMask.Button4)
				HandleDownButton();
			else if ((buttons & ButtonMask.Button6) == ButtonMask.Button6)
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


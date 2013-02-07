using System;
using System.Collections.Generic;

namespace RCCarService {
	public class MenuItem {

		private List<MenuItem> children = new List<MenuItem>();

		public MenuItem() : this("") {

		}

		public MenuItem(string title) {
			Title = title;
		}

		public delegate void MenuItemChosenEventHandler(MenuItem sender, EventArgs e);
		public event MenuItemChosenEventHandler MenuItemChosen;

		public string Title { get; private set; }
		internal MenuItem Parent { get; set; }

		public MenuItem[] ChildItems {
			get {
				return children.ToArray();
			}
		}

		public void AddChild(MenuItem item) {
			children.Add(item);
			item.Parent = this;
		}

		public void AddChildren(MenuItem[] items) {
			foreach (MenuItem item in items)
				AddChild(item);
		}

		public void RemoveChild(MenuItem item) {
			if (!children.Contains(item)) return;
			children.Remove(item);
			item.Parent = null;
		}

		public void RemoveChildren(MenuItem[] items) {
			foreach (MenuItem item in items)
				RemoveChild(item);
		}

		public void RemoveAllChildren() {
			RemoveChildren(children.ToArray());
		}

		public void HandleBeingChosen() {
			if (MenuItemChosen != null)
				MenuItemChosen(this, null);
		}

	}
}


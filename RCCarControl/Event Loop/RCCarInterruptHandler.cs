using System;

namespace RCCarControl {

	/// <summary>
	/// RC car interrupt handler.
	/// </summary>
	public class RCCarInterruptHandler {
		public RCCarInterruptHandler(){
		}

		public virtual bool ShouldTriggerInterruptWithState(RCCarState state) {
			throw new NotImplementedException();
		} 
	}
}


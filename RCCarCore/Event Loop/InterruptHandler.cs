using System;

namespace RCCarCore {

	/// <summary>
	/// RC car interrupt handler.
	/// </summary>
	public class InterruptHandler {
		public InterruptHandler(){
		}

		public virtual bool ShouldTriggerInterruptWithState(CarState state) {
			return false;
		} 
	}
}


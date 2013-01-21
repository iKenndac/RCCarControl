using System;

namespace RCCarControl {
	public class RCCarAIHandler {
		public RCCarAIHandler() {
		}

		public virtual bool PerformAIWork(RCCarState state, double cumulativeThrottleValue, double cumulativeSteeringValue, out double computedThrottleValue, out double computedSteeringValue) {
			throw new NotImplementedException();
		}
	}
}


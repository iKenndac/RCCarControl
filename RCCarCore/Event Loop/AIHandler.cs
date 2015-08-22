using System;

namespace RCCarCore {
	public class AIHandler {
		public AIHandler() {
		}

		public virtual bool PerformAIWork(CarState state, double cumulativeThrottleValue, double cumulativeSteeringValue, out double computedThrottleValue, out double computedSteeringValue) {
			computedSteeringValue = cumulativeSteeringValue;
			computedThrottleValue = cumulativeThrottleValue;
			return false;
		}
	}
}


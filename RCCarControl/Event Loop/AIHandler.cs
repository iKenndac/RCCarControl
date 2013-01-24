using System;

namespace RCCarControl {
	public class AIHandler {
		public AIHandler() {
		}

		public virtual bool PerformAIWork(CarState state, double cumulativeThrottleValue, double cumulativeSteeringValue, out double computedThrottleValue, out double computedSteeringValue) {
			computedSteeringValue = cumulativeSteeringValue;
			computedThrottleValue = cumulativeThrottleValue;
			System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(100));
			return false;
		}
	}
}


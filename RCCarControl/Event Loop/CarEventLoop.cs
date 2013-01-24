using System;
using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;

namespace RCCarControl {


	/// <summary>
	/// RC car event loop.
	/// The event loop runs on two threads - the first thread is a low-latency thread
	/// that watches for sensor updates and puts them through any installed interrupt
	/// handlers, which must also be low-latency.
	/// 
	/// If the interrupt handlers pass, the sensor updates are passed into a background
	/// runner that starts an iteration of the AI loop. This loop allows any installed AI
	/// behaviours to calculate updated steering and throttle values.
	/// 
	/// While the AI loop iteration is running, the interrupt thread carries on spinning.
	/// If an interrupt is fired, any currently running AI iterations are cancelled. If
	/// not, the next AI loop iteration is fired after the current one finishes. 
	/// 
	/// </summary>
	public class CarEventLoop {

		private const double kInterruptLoopTargetFrequency = 100.0; //Hz

		private List<InterruptHandler> _interrupts = new List<InterruptHandler>();
		private List<AIHandler> _aiHandlers = new List<AIHandler>();
		private ICarHardwareInterface _hardwareInterface;

		// Threading state ------
		private BackgroundWorker _aiWorker;
		private Thread _interruptThread;
		private ManualResetEvent _interruptThreadEndingEvent;
		private bool _interruptThreadCanRun = true;

		public CarEventLoop(ICarHardwareInterface hardwareInterface) {
			_hardwareInterface = hardwareInterface;
		}

		/// <summary>
		/// Starts the event loop. While the event loop is running,
		/// interrupts and ai handlers cannot be modified.
		/// </summary>
		public void StartLoop() {
			if (IsRunning)
				throw new Exception("Loop cannot be run multiple times at once.");

			if (_interrupts.Count == 0 && _aiHandlers.Count == 0)
				throw new Exception("Loop cannot be run with nothing to do.");

			_interruptThreadCanRun = true;
			_interruptThread = new Thread(new ThreadStart(RunInterruptThread));
			_interruptThread.Start();
		}

		/// <summary>
		/// Stops the event loop and resets vehicle state to natural.
		/// Note: This method blocks until everything is stopped.
		/// </summary>
		public void StopLoop() {
			if (!IsRunning) return;

			_interruptThreadEndingEvent = new ManualResetEvent(false);
			_interruptThreadCanRun = false;
			_interruptThreadEndingEvent.WaitOne();
			_interruptThread = null;
			_interruptThreadEndingEvent = null;

			_hardwareInterface.ApplyValueToServo(0.0, _hardwareInterface.SteeringServo);
			_hardwareInterface.ApplyValueToServo(0.0, _hardwareInterface.ThrottleServo);
		}

		/// <summary>
		/// Gets a value indicating whether the loop is running.
		/// </summary>
		/// <value>
		/// <c>true</c> if the loop is running; otherwise, <c>false</c>.
		/// </value>
		public bool IsRunning {
			get { return _interruptThread != null; }
		}

		/// <summary>
		/// Removes all interrupts and ai handlers. Can only be called
		/// when the loop isn't running.
		/// </summary>
		public void ResetState() {
			if (IsRunning)
				throw new Exception("Loop state cannot be altered while it's running.");

			_interrupts.Clear();
			_aiHandlers.Clear();
		}

		/// <summary>
		/// Adds an interrupt handler. Can only be called when the loop
		/// isn't running.
		/// </summary>
		/// <param name='handler'>
		/// The interrupt handler to add.
		/// </param>
		public void AddInterruptHandler(InterruptHandler handler) {
			if (IsRunning)
				throw new Exception("Loop state cannot be altered while it's running.");

			_interrupts.Add(handler);
		}

		/// <summary>
		/// Adds an AI handler. Can only be called when the loop isn't running.
		/// </summary>
		/// <param name='handler'>
		/// The AI handler to add.
		/// </param>
		public void AddAIHandler(AIHandler handler) {
			if (IsRunning)
				throw new Exception("Loop state cannot be altered while it's running.");
			
			_aiHandlers.Add(handler);
		}

		// -----

		private void RunInterruptThread() {

			while (_interruptThreadCanRun) {

				DateTime start = DateTime.Now;
				CarState currentState = _hardwareInterface.CreateState();

				foreach (InterruptHandler handler in _interrupts) {
					if (handler.ShouldTriggerInterruptWithState(currentState)) {
						HandleAllHaltInterrupt();
						CancelAIWorkerSync();
						return;
					}
				}

				if (_aiWorker == null) {
					// Collect any results and start a new worker.
	
					_aiWorker = new BackgroundWorker();
					_aiWorker.WorkerReportsProgress = false;
					_aiWorker.WorkerSupportsCancellation = true;
					_aiWorker.DoWork += RunAIWork;
					_aiWorker.RunWorkerCompleted += RunAIWorkCompleted;
					_aiWorker.RunWorkerAsync(currentState);
				}

				// Since we don't want to spin at 100% CPU usage, try to hit
				// our target loop frequency.
				DateTime end = DateTime.Now;
				TimeSpan duration = end - start;
				double timeToWait = (1000.0 / kInterruptLoopTargetFrequency) - duration.TotalMilliseconds;
				if (timeToWait > 0.0)
					Thread.Sleep((int)timeToWait);

			}

			CancelAIWorkerSync();
			_interruptThreadEndingEvent.Set();
		
		}

		private void CancelAIWorkerSync() {
			if (_aiWorker == null) return;

			_aiWorker.CancelAsync();
			// Check for null below since the _aiWorker gets cleared
			// out to null when it's finished.
			while (_aiWorker != null && _aiWorker.IsBusy)
				Thread.Sleep(TimeSpan.FromMilliseconds(1));

		}

		private void HandleAllHaltInterrupt() {
			// DON'T adjust steering here — if we're in a panicked state
			// there's a chance we could damage something if the wheels
			// are stuck.
			_hardwareInterface.ApplyValueToServo(0.0, _hardwareInterface.ThrottleServo);
		}

		/// <summary>
		/// Runs the AI loop.
		/// The AI loop runs all currently installed AI behaviours in serial. Each
		/// behaviour receives the steering and throttle values the vehicle is
		/// currently using, as well as a cumulative set of values so the behaviours
		/// can perform additive modifications to the vehicle's state.
		/// </summary>
		private void RunAIWork(object sender, DoWorkEventArgs e) {
			
			BackgroundWorker worker = (BackgroundWorker)sender;

			double cumulativeThrottle = 0.0;
			double cumulativeSteering = 0.0;
			CarState state = (CarState)e.Argument;

			foreach (AIHandler handler in _aiHandlers) {

				if (worker.CancellationPending) {
					e.Cancel = true;
					return;
				}

				double newThrottle, newSteering;

				if (handler.PerformAIWork(state, cumulativeThrottle, cumulativeSteering, out newThrottle, out newSteering)) {
					cumulativeThrottle = newThrottle;
					cumulativeSteering = newSteering;
				}
			}

			if (worker.CancellationPending)
				e.Cancel = true;

			e.Result = new Tuple<double, double>(cumulativeThrottle, cumulativeSteering);
		}

		private void RunAIWorkCompleted(object sender, RunWorkerCompletedEventArgs e) {
			_aiWorker = null;
		}
	}
}


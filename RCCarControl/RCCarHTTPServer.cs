using System;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;
using System.Collections.Generic;

namespace RCCarControl {
	
	/// <summary>
	/// RC car HTTP server, providing REST-style APIs for getting
	/// at the car's sensors.
	/// </summary>
	public class RCCarHTTPServer {

		private HttpListener Listener { get; set; }
		private Thread ResponseThread { get; set; }
		private IRCCarHardwareInterface Car { get; set; }

		public RCCarHTTPServer(IRCCarHardwareInterface car, int port) {

			if (car == null)
				throw new ArgumentNullException("car", "Car cannot be null.");

			Car = car;

			Listener = new HttpListener();
			Listener.Prefixes.Add(String.Format("http://+:{0}/", port));
			Listener.Start();

			ResponseThread = new Thread(HandleRequests);
			ResponseThread.Start();
		}

		~RCCarHTTPServer() {
			Listener.Stop();
		}

		private void HandleRequests() {
			while (Listener.IsListening) {
				var context = Listener.BeginGetContext(new AsyncCallback(ListenerCallback), Listener);
				context.AsyncWaitHandle.WaitOne();
			}
		}


		private void ListenerCallback(IAsyncResult result) {
			HttpListener listener = (HttpListener)result.AsyncState;
			// Call EndGetContext to complete the asynchronous operation.
			HttpListenerContext context = listener.EndGetContext(result);
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;
			string responseString = "";

			if (String.Compare(request.RawUrl, "/sensors/distances", StringComparison.OrdinalIgnoreCase) == 0) {
				response.StatusCode = 200;
				responseString = GenerateJSONForUltrasonicSensors();
			} else if (String.Compare(request.RawUrl, "/sensors/accelerometer", StringComparison.OrdinalIgnoreCase) == 0) {
				response.StatusCode = 200;
				responseString = GenerateJSONForAccelerometer();
			} else if (String.Compare(request.RawUrl, "/sensors", StringComparison.OrdinalIgnoreCase) == 0) {
				response.StatusCode = 200;
				responseString = GenerateJSONForAllSensors();
			} else {
				response.StatusCode = 404;
				responseString = "<html><body>404!</body></html>";
			}

			byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
			// Get a response stream and write the response to it.
			response.ContentLength64 = buffer.Length;
			System.IO.Stream output = response.OutputStream;
			output.Write(buffer,0,buffer.Length);
			// You must close the output stream.
			output.Close();
		}

		string GenerateJSONForAllSensors() {
			Dictionary<string, object> dictionaryRep = new Dictionary<string, object>();
			dictionaryRep["distances"] = CreateUltrasonicSensorRep();
			dictionaryRep["accelerometer"] = CreateAccelerometerRep();
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return serializer.Serialize(dictionaryRep);
		}

		string GenerateJSONForAccelerometer() {
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return serializer.Serialize(CreateAccelerometerRep());
		}

		string GenerateJSONForUltrasonicSensors() {
			JavaScriptSerializer serializer = new JavaScriptSerializer();
			return serializer.Serialize(CreateUltrasonicSensorRep());
		}

		Dictionary<String, int> CreateUltrasonicSensorRep() {
			int rearValue = Car.RearUltrasonicSensor.DistanceReadingCM;
			int frontLeftValue = Car.FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontLeft].DistanceReadingCM;
			int frontMiddleValue = Car.FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontMiddle].DistanceReadingCM;
			int frontRightValue = Car.FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontRight].DistanceReadingCM;
			
			Dictionary<String, int> dictionaryRep = new Dictionary<string, int>();
			dictionaryRep["RearDistance"] = rearValue;
			dictionaryRep["FrontLeftDistance"] = frontLeftValue;
			dictionaryRep["FrontMiddleDistance"] = frontMiddleValue;
			dictionaryRep["FrontRightDistance"] = frontRightValue;

			return dictionaryRep;
		}

		Dictionary<String, double> CreateAccelerometerRep() {
			Dictionary<String, double> dictionaryRep = new Dictionary<string, double>();
			dictionaryRep["x"] = Car.Accelerometor.X;
			dictionaryRep["y"] = Car.Accelerometor.Y;
			dictionaryRep["z"] = Car.Accelerometor.Z;
			return dictionaryRep;
		}


	}
}


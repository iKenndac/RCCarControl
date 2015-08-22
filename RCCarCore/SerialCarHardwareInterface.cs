using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace RCCarCore
{
    /// <summary>
    /// This interface defines the juicy stuff for the RC car hardware
    /// interface, just in case we want to have multiple implementations.
    /// </summary>
    public interface ICarHardwareInterface
    {
        UltrasonicSensor[] FrontUltrasonicSensors { get; }
        UltrasonicSensor RearUltrasonicSensor { get; }
        AccelerometorSensor Accelerometor { get; }
        Servo ThrottleServo { get; }
        Servo SteeringServo { get; }

        CarState CreateState();
        bool ApplyValueToServo(double value, Servo servo);
    }

    public enum UltrasonicSensorIndex : int
    {
        FrontLeft = 0,
        FrontMiddle = 1,
        FrontRight = 2
    }

    /// <summary>
    /// RC car hardware interface. This class is responsible for talking to
    /// the sensor module (i.e., an Arduino with stuff attached) and managing
    /// objects representing the various sensors and servos attached to it.
    /// </summary>
    public class SerialCarHardwareInterface : ICarHardwareInterface, IDisposable
    {

        public SerialCarHardwareInterface(String portPath)
        {
            // For now, we're hardcoding our sensors.
            RearUltrasonicSensor = new UltrasonicSensor();
            FrontUltrasonicSensors = new UltrasonicSensor[] {
                new UltrasonicSensor(),
                new UltrasonicSensor(),
                new UltrasonicSensor()
            };
            Accelerometor = new AccelerometorSensor();
            ThrottleServo = new Servo(this);
            SteeringServo = new Servo(this);

            StartSerialPortConnection();
        }

        public void Dispose()
        {
            CancelSerialReadTask();
            CloseSerialDevice();
        }

        public UltrasonicSensor[] FrontUltrasonicSensors { get; private set; }
        public UltrasonicSensor RearUltrasonicSensor { get; private set; }
        public AccelerometorSensor Accelerometor { get; private set; }
        public Servo ThrottleServo { get; private set; }
        public Servo SteeringServo { get; private set; }

        /// <summary>
        /// Creates an immutable copy of the interface's current state.
        /// </summary>
        /// <returns>
        /// The state.
        /// </returns>
        public CarState CreateState()
        {
            CarState state = new CarState();

            state.Accelerometer = new AccelerometorSensor();
            state.Accelerometer.SetValues(Accelerometor.X, Accelerometor.Y, Accelerometor.Z);

            state.RearUltrasonicSensor = new UltrasonicSensor();
            state.RearUltrasonicSensor.DistanceReadingCM = RearUltrasonicSensor.DistanceReadingCM;

            state.FrontUltrasonicSensors = new UltrasonicSensor[] {
                new UltrasonicSensor(),
                new UltrasonicSensor(),
                new UltrasonicSensor()
            };

            state.FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontLeft].DistanceReadingCM = FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontLeft].DistanceReadingCM;
            state.FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontMiddle].DistanceReadingCM = FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontMiddle].DistanceReadingCM;
            state.FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontRight].DistanceReadingCM = FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontRight].DistanceReadingCM;

            state.SteeringServo = new Servo(null);
            state.SteeringServo.Value = SteeringServo.Value;

            state.ThrottleServo = new Servo(null);
            state.ThrottleServo.Value = ThrottleServo.Value;

            return state;
        }

        public bool ApplyValueToServo(double value, Servo servo)
        {

            double throttleValue = ThrottleServo.Value;
            double steeringValue = SteeringServo.Value;

            if (servo == ThrottleServo) throttleValue = value;
            if (servo == SteeringServo) steeringValue = value;

            return WriteServoValuesToDevice(steeringValue, throttleValue);
        }

        private bool WriteServoValuesToDevice(double steering, double throttle)
        {
            // Serial protocol expects servo values in integral degrees from 
            // 0->180, while our objects have floating point -1.0->1.0.
            byte steeringValue = (byte)((steering * 90.0) + 90);
            byte throttleValue = (byte)((throttle * 90.0) + 90);

            byte[] message = new byte[5];
            message[0] = 0xBA;
            message[1] = 0xBE;
            message[2] = steeringValue;
            message[3] = throttleValue;
            message[4] = 0;

            // Checksum is the XOR of the message content.
            for (int index = 2; index <= 3; index++)
                message[4] ^= message[index];

            WriteToSerialPort(message);
            return true;
        }

        private void HandleLineFromSerialPort(string line)
        {

            if (line.StartsWith("DISTANCE:"))
            {

                string distanceString = line.Remove(0, "DISTANCE:".Length).Trim();
                string[] distanceStrings = distanceString.Split(',');
                List<int> distances = new List<int>();
                foreach (string distanceStringRepresentation in distanceStrings)
                {
                    try
                    {
                        distances.Add(Convert.ToInt32(distanceStringRepresentation));
                    }
                    catch
                    {
                        distances.Add(0);
                    }
                }

                HandleDistanceUpdate(distances.ToArray());
                return;
            }

            if (line.StartsWith("SERVO:"))
            {
                string servoMessage = line.Remove(0, "SERVO:".Length).Trim();
                HandleServoResponse(servoMessage);
                return;
            }

            if (line.StartsWith("ACCEL:"))
            {

                string accelString = line.Remove(0, "ACCEL:".Length).Trim();
                string[] accelStrings = accelString.Split(',');
                List<double> accels = new List<double>();
                foreach (string accelStringRepresentation in accelStrings)
                {
                    try
                    {
                        accels.Add(Convert.ToDouble(accelStringRepresentation));
                    }
                    catch
                    {
                        accels.Add(0.00);
                    }
                }

                HandleAccelerationUpdate(accels.ToArray());
                return;
            }
        }

        void HandleAccelerationUpdate(double[] accels)
        {

            if (accels.Length != 3)
            {
                Debug.WriteLine("Got unexpected number of acceleration values: {0}", accels.Length);
                return;
            }

            double x = accels[0];
            double y = accels[1];
            double z = accels[2];
            Accelerometor.SetValues(x, y, z);
        }

        void HandleServoResponse(string servoMessage)
        {
            if (servoMessage != "OK")
                Debug.WriteLine("WARNING: Got servo response: {0}", servoMessage);
        }

        private const int kRearDistanceSensorIndex = 3;
        private const int kFrontLeftDistanceSensorIndex = 1;
        private const int kFrontMiddleDistanceSensorIndex = 0;
        private const int kFrontRightDistanceSensorIndex = 2;

        private void HandleDistanceUpdate(int[] distances)
        {

            if (distances.Length != 4)
            {
                Debug.WriteLine("Got unexpected number of distances: {0}", distances.Length);
                return;
            }

            // Todo: Find a better way of defining which sensor is where.
            // This should probably be an implementation detail of the 
            // Arduino sketch.
            int rearDistanceValue = distances[kRearDistanceSensorIndex];
            int frontLeftDistanceValue = distances[kFrontLeftDistanceSensorIndex];
            int frontMiddleDistanceValue = distances[kFrontMiddleDistanceSensorIndex];
            int frontRightDistanceValue = distances[kFrontRightDistanceSensorIndex];

            RearUltrasonicSensor.DistanceReadingCM = rearDistanceValue;
            FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontLeft].DistanceReadingCM = frontLeftDistanceValue;
            FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontMiddle].DistanceReadingCM = frontMiddleDistanceValue;
            FrontUltrasonicSensors[(int)UltrasonicSensorIndex.FrontRight].DistanceReadingCM = frontRightDistanceValue;
        }

        #region Serial Port

        private SerialDevice serialDevice;
        private CancellationTokenSource serialReadCancellationToken;
        private DataWriter dataWriteObject = null;
        private DataReader dataReaderObject = null;
        private List<byte> buffer = new List<byte>();

        private async void StartSerialPortConnection()
        {
            string aqs = SerialDevice.GetDeviceSelector();
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(aqs);

            foreach (DeviceInformation info in devices)
            {
                Debug.WriteLine("Found device: {0}", (object)info.Name);

                serialDevice = await SerialDevice.FromIdAsync(info.Id);
                serialDevice.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serialDevice.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serialDevice.BaudRate = 9600;
                serialDevice.Parity = SerialParity.None;
                serialDevice.StopBits = SerialStopBitCount.One;
                serialDevice.DataBits = 8;

                // Create cancellation token object to close I/O operations when closing the device
                serialReadCancellationToken = new CancellationTokenSource();

                RecursivelyReadFromPortUntilCancelled();
            }
        }

        private void CloseSerialDevice()
        {
            if (serialDevice != null)
            {
                serialDevice.Dispose();
            }
            serialDevice = null;
        }

        #region Writing

        private async void WriteToSerialPort(byte[] data)
        {
            try
            {
                if (serialDevice != null)
                {
                    // Create the DataWriter object and attach to OutputStream
                    dataWriteObject = new DataWriter(serialDevice.OutputStream);

                    //Launch the WriteAsync task to perform the write
                    await WriteAsync(data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Write failed: {0}", (object)ex.Message);
            }
            finally
            {
                // Cleanup once complete
                if (dataWriteObject != null)
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }
        }

        private async Task WriteAsync(byte[] data)
        {
            // Load the text from the sendText input text box to the dataWriter object
            dataWriteObject.WriteBytes(data);

            // Launch an async task to complete the write operation
            Task<UInt32> storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

            UInt32 bytesWritten = await storeAsyncTask;
            if (bytesWritten > 0)
            {
                Debug.WriteLine("{0} bytes written successfully!", bytesWritten);
            }
        }

        #endregion

        #region Reading

        private async void RecursivelyReadFromPortUntilCancelled()
        {
            try
            {
                if (serialDevice != null)
                {

                    dataReaderObject = new DataReader(serialDevice.InputStream);
                    dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;
                    await WaitForMoreDataFromPort(serialReadCancellationToken.Token);
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name == "TaskCanceledException")
                {
                    CloseSerialDevice();
                }
                else
                {
                    Debug.WriteLine("Got error: {0}", (object)ex.Message);
                }
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                    if (serialDevice != null)
                    {
                        RecursivelyReadFromPortUntilCancelled();
                    }
                }
            }

        }

        private async Task WaitForMoreDataFromPort(CancellationToken cancelToken)
        {
            uint readBufferLength = 32;

            // If task cancellation was requested, comply
            cancelToken.ThrowIfCancellationRequested();

            // Create a task object to wait for data on the serialPort.InputStream
            Task<UInt32> loadAsyncTask = dataReaderObject.LoadAsync(readBufferLength).AsTask(cancelToken);

            // Launch the task and wait
            UInt32 bytesRead = await loadAsyncTask;
            if (bytesRead > 0)
            {
                byte[] receivedData = new byte[dataReaderObject.UnconsumedBufferLength];
                dataReaderObject.ReadBytes(receivedData);

                foreach (byte newByte in receivedData)
                {
                    buffer.Add(newByte);
                    if (newByte == 10)
                    {
                        string line = Encoding.UTF8.GetString(buffer.ToArray(), 0, buffer.Count);
                        line = line.Trim();
                        buffer.Clear();
                        Debug.WriteLine("Got line from device: {0}", (object)line);
                        HandleLineFromSerialPort(line);
                    }
                }
            }
        }

        private void CancelSerialReadTask()
        {
            if (serialReadCancellationToken != null)
            {
                if (!serialReadCancellationToken.IsCancellationRequested)
                {
                    serialReadCancellationToken.Cancel();
                }
            }
        }

        #endregion

        #endregion
    }
}


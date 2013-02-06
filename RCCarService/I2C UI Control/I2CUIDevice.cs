using System;
using System.IO;
using System.Text;
using System.Timers;
using Mono.Unix;
using Mono.Unix.Native;
using System.Runtime.InteropServices;

namespace RCCarService {
	public class I2CUIDevice : IDisposable  {

		[Flags]
		public enum ButtonMask : byte {
			ButtonNone = 0,
			Button6 = 1 << 0,
			Button5 = 1 << 1,
			Button4 = 1 << 2,
			Button3 = 1 << 3,
			Button2 = 1 << 4,
			Button1 = 1 << 5
		}

		public delegate void ButtonsPushedEventHandler(I2CUIDevice sender, ButtonMask buttons);
		public event ButtonsPushedEventHandler ButtonsPushed;

		public byte I2CDeviceSlaveAddress { get; private set; }
		public string I2CDevicePath { get; private set; }
		public int I2CFileDescriptor { get; set; }
		private Timer ButtonPollTimer { get; set; }

		public I2CUIDevice(string i2cPath, byte i2cAddress) {

			I2CDeviceSlaveAddress = i2cAddress;
			I2CDevicePath = i2cPath;

			I2CFileDescriptor = Syscall.open(I2CDevicePath, OpenFlags.O_RDWR);

			if (I2CFileDescriptor == -1) {
				Console.Out.WriteLine("Fatal: Couldn't open i2c device at {0}.", I2CDevicePath);
			}

			if (ioctl(I2CFileDescriptor, I2C_SLAVE, (byte)(i2cAddress >> 1)) != 0) {
				Console.Out.WriteLine("Warning (probably fatal): Couldn't set i2c address via ioctl().");
			}

			// Get any stored button states out of the way.
			PollButtonStates();

			ButtonPollTimer = new Timer(100);
			ButtonPollTimer.AutoReset = true;
			ButtonPollTimer.Elapsed += delegate(object sender, ElapsedEventArgs e) {
				PollButtonStates();
			};
			ButtonPollTimer.Enabled = true;
		}

		void IDisposable.Dispose() {

			if (ButtonPollTimer != null) {
				ButtonPollTimer.Enabled = false;
				ButtonPollTimer = null;
			}

			if (I2CFileDescriptor != 0) {
				Syscall.close(I2CFileDescriptor);
				I2CFileDescriptor = -1;
			}
		}

		/// <summary>
		/// Writes the given string to the display.
		/// </summary>
		/// <param name='str'>
		/// The string to write.
		/// </param>
		/// <param name='row'>
		/// The row number to start displaying the string.
		/// </param>
		/// <param name='column'>
		/// The column number to start displaying the string.
		/// </param>
		public void WriteString(string str, byte row, byte column) {
			// row is top 3 bits, column bottom 5
			int encodedLocation = ((row << 5) & 0xE0);
			encodedLocation |= (column & 0x1F);
			Write8BitRegister(WriteCommand.MoveCursor, (byte)encodedLocation);
			WriteStringToDevice(str);
		}

		/// <summary>
		/// Clears the screen.
		/// </summary>
		public void ClearScreen() {
			Write8BitRegister(WriteCommand.ClearScreen, 0x01);
		}


		// ---- Private methods and types

		enum WriteCommand : byte {
			MoveCursor = 0x11,
			WriteString = 0x00,
			ClearScreen = 0x10
		}
		
		enum ReadCommand : byte {
			PushedButtons = 0x31
		}

		// Calling ioctl()
		[DllImport("libc")]
		static extern int ioctl(int device, int identifier, byte newSlaveAddress);
		private const int I2C_SLAVE = 0x0703;  // Change slave address. Attn.: Slave address is 7 or 10 bits.

		private void PollButtonStates() {
			byte[] buf = new byte[1];
			buf[0] = (byte)ReadCommand.PushedButtons;
			byte[] ret = WriteToDevice(buf, 1);

			ButtonMask buttons = (ButtonMask)ret[0];
			if (buttons == ButtonMask.ButtonNone)
				return;

			if (ButtonsPushed != null) ButtonsPushed(this, buttons);
		}

		private void Write8BitRegister (WriteCommand register, byte value) {
			byte[] buf = new byte[2]; 
			buf[0] = (byte)register;
			buf[1] = value;
			WriteToDevice(buf);
		}

		private void WriteStringToDevice(string str) {
			byte[] buf = new byte[Encoding.ASCII.GetByteCount(str) + 1];
			buf[0] = (byte)WriteCommand.WriteString;
			Encoding.ASCII.GetBytes(str).CopyTo(buf, 1);
			WriteToDevice(buf);
		}

		private void WriteToDevice(byte[] data) {
			WriteToDevice(data, 0);
		}

		private byte[] WriteToDevice(byte[] data, int expectedResponseLength) {

			IntPtr writePtr = Marshal.AllocHGlobal(data.Length);
			Marshal.Copy(data, 0, writePtr, data.Length);
			long bytesWritten = Syscall.write(I2CFileDescriptor, writePtr, (ulong)data.Length);
			Marshal.FreeHGlobal(writePtr);
			writePtr = IntPtr.Zero;

			if (bytesWritten != data.Length) {
				Console.Out.WriteLine("Fatal: Couldn't write message to i2c device.");
				return null;
			}

			//I2CStream.Write(data, 0, data.Length);
			if (expectedResponseLength == 0)
				return null;

			IntPtr readPtr = Marshal.AllocHGlobal(expectedResponseLength);
			long readCount = Syscall.read(I2CFileDescriptor, readPtr, (ulong)expectedResponseLength);

			byte[] readData = new byte[readCount];
			Marshal.Copy(readPtr, readData, 0, (int)readCount);
			Marshal.FreeHGlobal(readPtr);
			return readData;
		}

	}
}


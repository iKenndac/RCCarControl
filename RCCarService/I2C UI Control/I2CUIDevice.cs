using System;
using System.IO;
using System.Text;
using System.Timers;
using Mono.Unix;
using Mono.Unix.Native;
using System.Runtime.InteropServices;

namespace RCCarService {
	public class I2CUIDevice : IDisposable  {

		public static string StringForCharacter(CustomCharacter character) {
			return Encoding.ASCII.GetString(new byte[] { (byte)character });
		}

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

		public enum CustomCharacter : byte {
			Tick = 0,
			Cross = 1,
			Left = 2,
			Right = 3,
			Up = 4,
			Down = 5,
			Ellipsis = 6,
			Blank = 0x20 // ASCII space
		}

		public enum ButtonSymbolPosition : byte {
			Button1 = 0,
			Button2 = 3,
			Button3 = 6,
			Button4 = 9,
			Button5 = 12,
			Button6 = 15
		}

		public delegate void ButtonsPushedEventHandler(I2CUIDevice sender, ButtonMask buttons);

		/// <summary>
		/// Fires when buttons are pushed on the device.
		/// </summary>
		public event ButtonsPushedEventHandler ButtonsPushed;

		public byte I2CDeviceSlaveAddress { get; private set; }
		public string I2CDevicePath { get; private set; }
		public int I2CFileDescriptor { get; set; }

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

			// Custom characters
			DefineCharacters();
			ClearScreen();

			// Get any stored button states out of the way.
			PollButtonStates();

			ButtonPollTimer = new Timer(250);
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

		public void WriteButtonSymbol(CustomCharacter character, ButtonSymbolPosition button) {
			WriteString(I2CUIDevice.StringForCharacter(character), 1, (byte)button);
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
			ClearScreen = 0x10,
			ScreenDirect = 0x01
		}
		
		enum ReadCommand : byte {
			PushedButtons = 0x31
		}

		private Timer ButtonPollTimer { get; set; }

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

			// If we're not expecting any data back, we shouldn't
			// try to read since we may well block.
			if (expectedResponseLength == 0)
				return null;

			IntPtr readPtr = Marshal.AllocHGlobal(expectedResponseLength);
			long readCount = Syscall.read(I2CFileDescriptor, readPtr, (ulong)expectedResponseLength);

			byte[] readData = new byte[readCount];
			Marshal.Copy(readPtr, readData, 0, (int)readCount);
			Marshal.FreeHGlobal(readPtr);
			return readData;
		}

		private void DefineCharacters() {
			//http://omerk.github.com/lcdchargen/

			byte[] tickChar = new byte[] {
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000001", 2),
				Convert.ToByte("00000010", 2),
				Convert.ToByte("00010100", 2),
				Convert.ToByte("00001000", 2)
			};

			byte[] crossChar = new byte[] {
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00010001", 2),
				Convert.ToByte("00001010", 2),
				Convert.ToByte("00000100", 2),
				Convert.ToByte("00001010", 2),
				Convert.ToByte("00010001", 2)
			};

			byte[] downChar = new byte[] {
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000100", 2),
				Convert.ToByte("00000100", 2),
				Convert.ToByte("00011111", 2),
				Convert.ToByte("00001110", 2),
				Convert.ToByte("00000100", 2)
			};

			byte[] upChar = new byte[] {
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000100", 2),
				Convert.ToByte("00001110", 2),
				Convert.ToByte("00011111", 2),
				Convert.ToByte("00000100", 2),
				Convert.ToByte("00000100", 2)
			};

			byte[] leftChar = new byte[] {
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000100", 2),
				Convert.ToByte("00001100", 2),
				Convert.ToByte("00011111", 2),
				Convert.ToByte("00001100", 2),
				Convert.ToByte("00000100", 2)
			};

			byte[] rightChar = new byte[] {
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000100", 2),
				Convert.ToByte("00000110", 2),
				Convert.ToByte("00011111", 2),
				Convert.ToByte("00000110", 2),
				Convert.ToByte("00000100", 2)
			};

			byte[] ellipsisChar = new byte[] {
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00000000", 2),
				Convert.ToByte("00010101", 2),
				Convert.ToByte("00000000", 2)
			};

			DefineCharacter(tickChar, CustomCharacter.Tick);
			DefineCharacter(crossChar, CustomCharacter.Cross);
			DefineCharacter(leftChar, CustomCharacter.Left);
			DefineCharacter(rightChar, CustomCharacter.Right);
			DefineCharacter(upChar, CustomCharacter.Up);
			DefineCharacter(downChar, CustomCharacter.Down);
			DefineCharacter(ellipsisChar, CustomCharacter.Ellipsis);
		}

		private void DefineCharacter(byte[] character, CustomCharacter characterNum) {
			//http://www.bitwizard.nl/wiki/index.php/Lcd_protocol_1.6
			//http://code.google.com/p/arduino/source/browse/trunk/libraries/LiquidCrystal/LiquidCrystal.h

			byte screenDirectDefineCharCommand = (byte)(0x40 | ((byte)characterNum << 3));

			byte[] buf = new byte[2];
			buf[0] = (byte)WriteCommand.ScreenDirect;
			buf[1] = screenDirectDefineCharCommand;

			WriteToDevice(buf);
			WriteToDevice(character);
			Write8BitRegister(WriteCommand.MoveCursor, 0);
		}
	}
}


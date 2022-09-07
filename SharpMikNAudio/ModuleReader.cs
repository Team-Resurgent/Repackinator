using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SharpMik.IO
{

	/*
	 * Needs to be tidy up, removal of functions that are not needed any more
	 * And chaning of function headers to make more sense.
	 * 
	 * Also to not throw exceptions when hitting EOF, instead passing back how
	 * much data was read.
	 * 
	 */
	public class ModuleReader : BinaryReader
	{
		public ModuleReader(Stream baseStream)
			: base(baseStream)
		{

		}

		#region stream functions
		public bool Seek(int offset, SeekOrigin origin)
		{
			BaseStream.Seek(offset, origin);

			return BaseStream.Position < BaseStream.Length;
		}

		public virtual int Tell()
		{
			try
			{
				return (int)(BaseStream.Position);
			}
			catch (System.IO.IOException)
			{
				return -1;
			}
		}

		public virtual bool isEOF()
		{
			try
			{
				return (BaseStream.Position > BaseStream.Length);
			}
			catch (System.IO.IOException)
			{
				return true;
			}
		}

		public void Rewind()
		{
			Seek(0, SeekOrigin.Begin);
		}
		#endregion

		#region byte / sbyte functions
		public virtual byte Read_byte()
		{
			try
			{
				return (byte)this.ReadByte();
			}
			catch
			{
				return byte.MaxValue;
				//throw ioe1;
			}
		}

		public virtual sbyte Read_sbyte()
		{
			try
			{
				return (sbyte)this.ReadByte();
			}
			catch (System.IO.IOException ioe1)
			{
				throw ioe1;
			}
		}


		public virtual bool Read_bytes(byte[] buffer, int number)
		{
			int pos = 0; 
			while (number > 0)
			{
				buffer[pos++] = Read_byte(); 
				number--;
			} 
			
			return !isEOF();
		}

		public virtual bool Read_bytes(sbyte[] buffer, int number)
		{
			int pos = 0;
			while (number > 0)
			{
				buffer[pos++] = (sbyte)Read_byte();
				number--;
			}

			return !isEOF();
		}

		public virtual bool Read_bytes(ushort[] buffer, int number)
		{
			int pos = 0; while (number > 0)
			{
				buffer[pos++] = (ushort)Read_byte();
				number--;
			}

			return !isEOF();
		}


		public virtual bool Read_bytes(char[] buffer, int number)
		{
			int pos = 0; while (number > 0)
			{
				buffer[pos++] = (char)Read_byte();
				number--;
			}

			return !isEOF();
		}


		public virtual bool Read_bytes(short[] buffer, int number)
		{
			int pos = 0; while (number > 0)
			{
				buffer[pos++] = (short)Read_byte();
				number--;
			}

			return !isEOF();
		}
		#endregion

		#region short / ushort functions
		public virtual ushort Read_Motorola_ushort()
		{
			byte b1 = this.ReadByte();
			byte b2 = this.ReadByte();

			int ushort1 = (int)b1;
			int ushort2 = (int)b2;

			ushort result = (ushort)(ushort1 << 8);
			result = (ushort)(result | ushort2);
			return result;
		}

		public virtual ushort Read_Intel_ushort()
		{
			ushort result = Read_byte();
			result |= (ushort)(Read_byte() << 8);
			return result;
		}


		public virtual short Read_Motorola_short()
		{
			short result = (short)(Read_byte() << 8);
			result |= (short)Read_byte();
			return result;
		}

		public virtual bool Read_Intel_ushorts(ushort[] buffer, int number)
		{
			int pos = 0; while (number > 0)
			{
				buffer[pos++] = Read_Intel_ushort(); 
				number--;
			} 
			return !isEOF();
		}

		public virtual bool Read_Intel_ushorts(ushort[] buffer,int offset, int number)
		{
			int pos = 0; 
			while (number > 0 && offset + pos < buffer.Length)
			{
				buffer[offset + pos++] = Read_Intel_ushort();
				number--;
			}
			return !isEOF();
		}

		public virtual short Read_Intel_short()
		{
			short result = Read_byte();
			result |= (short)(Read_byte() << 8);
			return result;
		}

		public virtual bool read_Motorola_shorts(short[] buffer, int number)
		{
			int pos = 0; while (number > 0)
			{
				buffer[pos++] = Read_Motorola_short(); number--;
			} return !isEOF();
		}

		public virtual bool read_Intel_shorts(short[] buffer, int number)
		{
			int pos = 0; while (number > 0)
			{
				buffer[pos++] = Read_Intel_short(); number--;
			} return !isEOF();
		}
		#endregion



		#region int / uint functions
		public virtual uint Read_Motorola_uint()
		{
			int result = (Read_Motorola_ushort()) << 16;
			result |= Read_Motorola_ushort();
			return (uint)result;
		}

		public virtual int Read_Motorola_uints(uint[] buffer, int number)
		{
			int pos = 0; 
			while (number > 0)
			{
				buffer[pos++] = Read_Motorola_uint();
				number--;
			}

			return pos;
		}

		public virtual uint Read_Intel_uint()
		{
			uint result = Read_Intel_ushort();
			result |= ((uint)Read_Intel_ushort()) << 16;
			return result;
		}

		public virtual bool Read_Intel_uints(uint[] buffer, int number)
		{
			int pos = 0; while (number > 0)
			{
				buffer[pos++] = Read_Intel_uint();
				number--;
			}
			return !isEOF();
		}


		public virtual int Read_Motorola_int()
		{
			return ((int)Read_Motorola_uint());
		}

		public virtual int Read_Intel_int()
		{
			return ((int)Read_Intel_uint());
		}
		#endregion


		public string Read_String(int length)
		{
			try
			{
				byte[] tmpBuffer = new byte[length];
				this.Read(tmpBuffer, 0, length);

				return System.Text.UTF8Encoding.UTF8.GetString(tmpBuffer, 0, length).Trim(new char[] {'\0'});
			}
			catch (System.IO.IOException ioe1)
			{
				throw ioe1;
			}
		}
	}
}

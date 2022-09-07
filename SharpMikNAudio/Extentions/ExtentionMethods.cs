using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SharpMik.Extentions
{
	public static class ExtentionMethods
	{
		public static void Write(this BinaryWriter value, sbyte[] buffer, int start, int count)
		{
			for (int i = 0; i < count; i++)
			{
				value.Write(buffer[start + i]);
			}
		}

		public static void Write(this MemoryStream value, sbyte[] buffer, int start, int count)
		{
			for (int i = 0; i < count; i++)
			{				
				value.WriteByte((byte)buffer[start + i]);
			}
		}


		public static void Memset(this byte[] buf, byte value, int count)
		{
			for (int i = 0; i < count && i < buf.Length; i++)
			{
				buf[i] = value;
			}
		}

		public static void Memset(this sbyte[] buf, sbyte value, int count)
		{
			for (int i = 0; i < count && i < buf.Length; i++)
			{
				buf[i] = value;
			}
		}

		public static void Memset(this ushort[] buf, ushort value, int count)
		{
			for (int i = 0; i < count && i < buf.Length; i++)
			{
				buf[i] = value;
			}
		}
	}
}

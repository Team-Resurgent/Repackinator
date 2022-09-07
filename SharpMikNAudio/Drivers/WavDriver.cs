using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SharpMik.Player;
using SharpMik.Extentions;
using System.Diagnostics;

namespace SharpMik.Drivers
{
	public class WavDriver : VirtualSoftwareDriver
    {
		BinaryWriter? m_FileStream;

		string m_FileName = "music.wav";

		sbyte[]? m_Audiobuffer;

		public static uint BUFFERSIZE = 32768;
		uint dumpsize;

		public WavDriver()
		{
			m_Next = null;
			m_Name = "Disk Wav Writer";
			m_Version = "Wav disk writer (music.wav) v1.0";
			m_HardVoiceLimit = 0;
			m_SoftVoiceLimit = 255;
			m_AutoUpdating = false;
		}

		public override void CommandLine(string command)
		{
			if (!string.IsNullOrEmpty(command))
			{
				m_FileName = command;
			}			
		}

		public override bool IsPresent()
		{
			return true;
		}

		public override bool Init()
		{
			try
			{
				FileStream stream = new FileStream(m_FileName,FileMode.Create);
				m_FileStream = new BinaryWriter(stream);
				m_Audiobuffer = new sbyte[BUFFERSIZE];

				ModDriver.Mode = (ushort)( ModDriver.Mode | SharpMikCommon.DMODE_SOFT_MUSIC | SharpMikCommon.DMODE_SOFT_SNDFX);

				putheader();

				return base.Init();
			}
			catch (System.Exception ex)
			{
				throw ex;
			}
		}

		public override void Exit()
		{
			try
			{
				putheader();
				base.Exit();
				//putheader();
				if (m_FileStream != null)
				{
					m_FileStream.Close();
					m_FileStream.Dispose();
					m_FileStream = null;
				}
			}
			catch (System.Exception ex)
			{
				throw ex;
			}

		}
		int loc = 0;

		public override void Update()
		{
			uint done = WriteBytes(m_Audiobuffer, BUFFERSIZE);
			m_FileStream.Write(m_Audiobuffer,0,(int)done);										
			dumpsize += done;
			loc++;
		}


		void putheader()
		{
			m_FileStream.Seek(0,SeekOrigin.Begin);
			m_FileStream.Write("RIFF".ToCharArray());
			m_FileStream.Write((uint)(dumpsize + 44));
			m_FileStream.Write("WAVEfmt ".ToCharArray());
			m_FileStream.Write((uint)16);
			m_FileStream.Write((ushort)1);
			ushort channelCount = (ushort)((ModDriver.Mode & SharpMikCommon.DMODE_STEREO) == SharpMikCommon.DMODE_STEREO ? 2 : 1);
			ushort numberOfBytes = (ushort)((ModDriver.Mode & SharpMikCommon.DMODE_16BITS) == SharpMikCommon.DMODE_16BITS ? 2 : 1 );

			m_FileStream.Write(channelCount);
			m_FileStream.Write((uint)ModDriver.MixFrequency);
			int blah = ModDriver.MixFrequency * channelCount * numberOfBytes;
			m_FileStream.Write((uint)(blah));
			m_FileStream.Write((ushort)(channelCount * numberOfBytes));
			m_FileStream.Write((ushort)((ModDriver.Mode & SharpMikCommon.DMODE_16BITS) == SharpMikCommon.DMODE_16BITS ? 16 : 8));
			m_FileStream.Write("data".ToCharArray());
			m_FileStream.Write((uint)dumpsize);			
		}
	}
}

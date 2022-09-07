using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpMik.Interfaces;
using System.IO;


using SharpMik.Extentions;
using SharpMik;
using SharpMik.Attributes;

namespace SharpMik.Loaders
{
    [ModFileExtentions(".s3m")]
    public class S3MLoader : IModLoader
	{
		/* header */
		class S3MHEADER
		{
			public string songname;
			public byte t1a;
			public byte type;
			public byte[] unused1 = new byte[2];
			public ushort ordnum;
			public ushort insnum;
			public ushort patnum;
			public ushort flags;
			public ushort tracker;
			public ushort fileformat;
			public string scrm;
			public byte mastervol;
			public byte initspeed;
			public byte inittempo;
			public byte mastermult;
			public byte ultraclick;
			public byte pantable;
			public byte[] unused2 = new byte[8];
			public ushort special;
			public byte[] channels = new byte[32];
		};

		/* sample information */
		class S3MSAMPLE 
		{
			public byte type;
			public string filename;
			public byte memsegh;
			public ushort memsegl;
			public uint length;
			public uint loopbeg;
			public uint loopend;
			public byte volume;
			public byte dsk;
			public byte pack;
			public byte flags;
			public uint c2spd;
			public byte[] unused = new byte[12];
			public string sampname;
			public string scrs;
		};

		class S3MNOTE 
		{
			public byte note, ins, vol, cmd, inf;

			public void Clear()
			{
				note = ins = vol = cmd = inf = 255;
			}
		};

		/*========== Loader variables */

		static S3MNOTE   []? s3mbuf  = null; /* pointer to a complete S3M pattern */
		static S3MHEADER? mh      = null;
		static ushort     []? paraptr = null; /* parapointer array (see S3M docs) */
		static uint tracker;	/* tracker id */

		/* tracker identifiers */
		static int NUMTRACKERS = 4;
		static string[] S3M_Version = 
		{
			"Screamtracker x.xx",
			"Imago Orpheus x.xx (S3M format)",
			"Impulse Tracker x.xx (S3M format)",
			"Unknown tracker x.xx (S3M format)",
			"Impulse Tracker 2.14p3 (S3M format)",
			"Impulse Tracker 2.14p4 (S3M format)"
		};
		/* version number position in above array */
		static int []numeric={14,14,16,16};



		public S3MLoader()
			: base()
		{
			m_ModuleType = "S3M";
			m_ModuleVersion = "S3M (Scream Tracker 3)";
		}


		public override bool Test()
		{
            string id;

			m_Reader.Seek(0x02c,SeekOrigin.Begin);
			id = m_Reader.Read_String(4);

			return id == "SCRM";
		}

		public override bool Init()
		{
			s3mbuf = new S3MNOTE[32 * 64];

			for (int i = 0; i < s3mbuf.Length; i++)
			{
				s3mbuf[i] = new S3MNOTE();
			}

			poslookup = new byte[256];
			poslookup.Memset(byte.MaxValue, 256);

			mh = new S3MHEADER();
			return true;
		}

		public override void Cleanup()
		{
			s3mbuf = null;
			paraptr = null;
			poslookup = null;
			mh = null;
			origpositions   = null;
		}

		bool S3M_GetNumChannels()
		{
			int row=0,flag,ch;

			while(row<64) 
			{
				flag=m_Reader.Read_byte();

				if(m_Reader.isEOF()) 
				{
					m_LoadError = MMERR_LOADING_PATTERN;
					return false;
				}

				if (flag != 0)
				{
					ch = flag & 31;
					if (mh.channels[ch] < 32)
						remap[ch] = 0;

					if ((flag & 32) != 0)
					{
						m_Reader.Read_byte();
						m_Reader.Read_byte();
					}

					if ((flag & 64) != 0)
						m_Reader.Read_byte();

					if ((flag & 128) != 0)
					{
						m_Reader.Read_byte();
						m_Reader.Read_byte();
					}
				}
				else
				{
					row++;
				}
			}

            return true;
		}    

		bool S3M_ReadPattern()
		{
			int row=0,flag,ch;
			S3MNOTE n;
			S3MNOTE dummy = new S3MNOTE();

			/* clear pattern data */
			for(int i=0;i<s3mbuf.Length;i++)
			{
				s3mbuf[i].Clear();
			}

			while(row<64) 
			{
				flag=m_Reader.Read_byte();

				if(m_Reader.isEOF()) 
				{
					m_LoadError = MMERR_LOADING_PATTERN;
					return false;
				}

				if (flag != 0)
				{
					ch = remap[flag & 31];

					if (ch != -1)
						n = s3mbuf[(64U * ch) + row];
					else
						n = dummy;

					if ((flag & 32) != 0)
					{
						n.note = m_Reader.Read_byte();
						n.ins = m_Reader.Read_byte();
					}

					if ((flag & 64) != 0)
					{
						n.vol = m_Reader.Read_byte();
						if (n.vol > 64) n.vol = 64;
					}

					if ((flag & 128) != 0)
					{
						n.cmd = m_Reader.Read_byte();
						n.inf = m_Reader.Read_byte();
					}
				}
				else
				{
					row++;
				}
			}
			return true;
		}

		byte[] S3M_ConvertTrack(S3MNOTE[] tr, int location)
		{
			int t;

			UniReset();
			for(t=0;t<64;t++) 
			{
				byte note,ins,vol;

				note = tr[t + location].note;
				ins = tr[t + location].ins;
				vol = tr[t + location].vol;

				if((ins != 0)&&(ins!=255))
					UniInstrument(ins-1);
				
				if(note!=255) 
				{
					if(note==254) 
					{
						UniPTEffect(0xc,0);	/* note cut command */
						vol=255;
					} else
						UniNote(((note>>4)*SharpMikCommon.Octave)+(note&0xf));	/* normal note */
				}
				if(vol<255) 
					UniPTEffect(0xc,vol);

				S3MIT_ProcessCmd(tr[t + location].cmd, tr[t + location].inf, (uint)(tracker == 1 ? SharpMikCommon.S3MIT_OLDSTYLE | SharpMikCommon.S3MIT_SCREAM : SharpMikCommon.S3MIT_OLDSTYLE));
				UniNewline();
			}
			return UniDup();
		}


		public override bool Load(int curious)
		{
			int t,u,track = 0;
			SAMPLE q;
			byte[] pan = new byte[32];

			/* try to read module header */
			mh.songname = m_Reader.Read_String(28);
			mh.t1a         =m_Reader.Read_byte();
			mh.type        =m_Reader.Read_byte();
			m_Reader.Read_bytes(mh.unused1, 2);
			mh.ordnum		= m_Reader.Read_Intel_ushort();
			mh.insnum      =m_Reader.Read_Intel_ushort();
			mh.patnum      =m_Reader.Read_Intel_ushort();
			mh.flags       =m_Reader.Read_Intel_ushort();
			mh.tracker     =m_Reader.Read_Intel_ushort();
			mh.fileformat  =m_Reader.Read_Intel_ushort();
			mh.scrm = m_Reader.Read_String(4);
			mh.mastervol   =m_Reader.Read_byte();
			mh.initspeed   =m_Reader.Read_byte();
			mh.inittempo   =m_Reader.Read_byte();
			mh.mastermult  =m_Reader.Read_byte();
			mh.ultraclick  =m_Reader.Read_byte();
			mh.pantable    =m_Reader.Read_byte();
			m_Reader.Read_bytes(mh.unused2, 8);
			mh.special     =m_Reader.Read_Intel_ushort();
			m_Reader.Read_bytes(mh.channels, 32);

			if(m_Reader.isEOF()) 
			{
				m_LoadError = MMERR_LOADING_HEADER;
				return false;
			}

			/* then we can decide the module type */
			tracker=(uint)(mh.tracker>>12);
			if((tracker == 0)||(tracker>=NUMTRACKERS))
				tracker=(uint)(NUMTRACKERS-1); /* unknown tracker */
			else {
				if(mh.tracker>=0x3217)
					tracker=(uint)(NUMTRACKERS+1); /* IT 2.14p4 */
				else if(mh.tracker>=0x3216)
					tracker=(uint)(NUMTRACKERS); /* IT 2.14p3 */
				else tracker--;
			}
			
			
			if(tracker<NUMTRACKERS) 
			{
				char[] version = S3M_Version[tracker].ToCharArray();

				version[numeric[tracker]] = (char)(((mh.tracker >> 8) & 0xf) + '0');
				version[numeric[tracker] + 2] =(char)(((mh.tracker >> 4) & 0xf) + '0');
				version[numeric[tracker] + 3] = (char)(((mh.tracker) & 0xf) + '0');

				m_Module.modtype = new string(version);
			}
			else
			{
				m_Module.modtype = S3M_Version[tracker];
			}
			/* set module variables */
			m_Module.songname    = mh.songname;
			m_Module.numpat      = mh.patnum;
			m_Module.reppos      = 0;
			m_Module.numins      = m_Module.numsmp = mh.insnum;
			m_Module.initspeed   = mh.initspeed;
			m_Module.inittempo   = mh.inittempo;
			m_Module.initvolume  = (byte)(mh.mastervol<<1);
			m_Module.flags |= SharpMikCommon.UF_ARPMEM | SharpMikCommon.UF_PANNING;
			
			if((mh.tracker==0x1300)||(mh.flags&64) != 0)
				m_Module.flags |= SharpMikCommon.UF_S3MSLIDES;

			m_Module.bpmlimit    = 32;

			/* read the order data */
			m_Module.positions = new ushort[mh.ordnum];
			origpositions = new ushort[mh.ordnum];			

			for(t=0;t<mh.ordnum;t++) 
			{
				origpositions[t]=m_Reader.Read_byte();
				if((origpositions[t]>=mh.patnum)&&(origpositions[t]<254))
					origpositions[t]=255/*mh.patnum-1*/;
			}

			if(m_Reader.isEOF()) 
			{
				m_LoadError = MMERR_LOADING_HEADER;
				return false;
			}

			poslookupcnt=mh.ordnum;
			S3MIT_CreateOrders(curious);

			paraptr = new ushort[m_Module.numins + m_Module.numpat];

			/* read the instrument+pattern parapointers */
			m_Reader.Read_Intel_ushorts(paraptr, m_Module.numins + m_Module.numpat);

			if(mh.pantable==252) 
			{
				/* read the panning table (ST 3.2 addition.  See below for further
				   portions of channel panning [past reampper]). */
				m_Reader.Read_bytes(pan, 32);
			}

			if(m_Reader.isEOF()) 
			{
				m_LoadError = MMERR_LOADING_HEADER;
				return false;
			}

			m_Module.AllocSamples();
			/* load samples */
			//q = m_Module.samples;
			for(t=0;t<m_Module.numins;t++) 
			{
				q = m_Module.samples[t];
				S3MSAMPLE s = new S3MSAMPLE();

				/* seek to instrument position */
				m_Reader.Seek(paraptr[t] << 4, SeekOrigin.Begin);

				/* and load sample info */
				s.type      =m_Reader.Read_byte();
				s.filename	= m_Reader.Read_String(12);
				s.memsegh   =m_Reader.Read_byte();
				s.memsegl   =m_Reader.Read_Intel_ushort();
				s.length    =m_Reader.Read_Intel_uint();
				s.loopbeg	= m_Reader.Read_Intel_uint();
				s.loopend	= m_Reader.Read_Intel_uint();
				s.volume    =m_Reader.Read_byte();
				s.dsk       =m_Reader.Read_byte();
				s.pack      =m_Reader.Read_byte();
				s.flags     =m_Reader.Read_byte();
				s.c2spd		= m_Reader.Read_Intel_uint();
				m_Reader.Read_bytes(s.unused,12);
				s.sampname = m_Reader.Read_String(28);
				s.scrs = m_Reader.Read_String(4);

				/* ScreamTracker imposes a 64000 bytes (not 64k !) limit */
				if (s.length > 64000 && tracker == 1)
					s.length = 64000;

				if(m_Reader.isEOF()) {
					m_LoadError = MMERR_LOADING_SAMPLEINFO;
					return false;
				}

				q.samplename = s.sampname;
				q.speed      = s.c2spd;
				q.length     = s.length;
				q.loopstart  = s.loopbeg;
				q.loopend    = s.loopend;
				q.volume     = s.volume;
				q.seekpos    = (uint)((((long)s.memsegh)<<16|s.memsegl)<<4);

				if((s.flags&1) != 0) 
					q.flags |= SharpMikCommon.SF_LOOP;
				if((s.flags&4) != 0)
					q.flags |= SharpMikCommon.SF_16BITS;
				if (mh.fileformat == 1) 
					q.flags |= SharpMikCommon.SF_SIGNED;

				/* don't load sample if it doesn't have the SCRS tag */
				if(s.scrs != "SCRS") 
					q.length = 0;
			}

			/* determine the number of channels actually used. */
			m_Module.numchn = 0;
			remap.Memset(-1, 32);

			for(t=0;t<m_Module.numpat;t++) 
			{
				/* seek to pattern position (+2 skip pattern length) */
				m_Reader.Seek(((paraptr[m_Module.numins+t])<<4)+2, SeekOrigin.Begin);
				if(!S3M_GetNumChannels()) 
					return false;
			}

			/* build the remap array  */
			for (t = 0; t < 32; t++)
			{
				if (remap[t] == 0)
				{
					remap[t] = (sbyte)(m_Module.numchn++);
				}
			}

			/* set panning positions after building remap chart! */
			for(t=0;t<32;t++) 
				if((mh.channels[t]<32)&&(remap[t]!=-1)) {
					if(mh.channels[t]<8)
						m_Module.panning[remap[t]]=0x30;
					else
						m_Module.panning[remap[t]]=0xc0;
				}
			if(mh.pantable==252)
				/* set panning positions according to panning table (new for st3.2) */
				for(t=0;t<32;t++)
					if((pan[t]&0x20) != 0&&(mh.channels[t]<32)&&(remap[t]!=-1))
						m_Module.panning[remap[t]]=(ushort)((pan[t]&0xf)<<4);

			/* load pattern info */
			m_Module.numtrk=(ushort)(m_Module.numpat*m_Module.numchn);

			m_Module.AllocTracks();
			m_Module.AllocPatterns();

			for(t=0;t<m_Module.numpat;t++) 
			{
				/* seek to pattern position (+2 skip pattern length) */
				m_Reader.Seek(((paraptr[m_Module.numins + t]) << 4) + 2, SeekOrigin.Begin);
				if(!S3M_ReadPattern()) 
					return false;

				for (u = 0; u < m_Module.numchn; u++)
				{
					m_Module.tracks[track++] = S3M_ConvertTrack(s3mbuf,u * 64);
				}
			}

			return true;
		}



		public override string LoadTitle()
		{
            string name;
			m_Reader.Seek(0,SeekOrigin.Begin);
			name = m_Reader.Read_String(28);

			return name;
		}
	}
}

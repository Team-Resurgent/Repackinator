using SharpMik.Attributes;
using SharpMik.Extentions;
using SharpMik.Interfaces;

namespace SharpMik.Loaders
{
    [ModFileExtentions(".it")]
    public class ITLoader : IModLoader
    {
        /* header */
        class ITHEADER
        {
            public string songname;
            public byte[] blank01 = new byte[2];
            public ushort ordnum;
            public ushort insnum;
            public ushort smpnum;
            public ushort patnum;
            public ushort cwt;      /* Created with tracker (y.xx = 0x0yxx) */
            public ushort cmwt;     /* Compatible with tracker ver > than val. */
            public ushort flags;
            public ushort special;  /* bit 0 set = song message attached */
            public byte globvol;
            public byte mixvol;     /* mixing volume [ignored] */
            public byte initspeed;
            public byte inittempo;
            public byte pansep;     /* panning separation between channels */
            public byte zerobyte;
            public ushort msglength;
            public uint msgoffset;
            public byte[] blank02 = new byte[4];
            public byte[] pantable = new byte[64];
            public byte[] voltable = new byte[64];
        };

        /* sample information */
        class ITSAMPLE
        {
            public string filename;
            public byte zerobyte;
            public byte globvol;
            public byte flag;
            public byte volume;
            public byte panning;
            public string sampname;
            public ushort convert;  /* sample conversion flag */
            public uint length;
            public uint loopbeg;
            public uint loopend;
            public uint c5spd;
            public uint susbegin;
            public uint susend;
            public uint sampoffset;
            public byte vibspeed;
            public byte vibdepth;
            public byte vibrate;
            public byte vibwave;    /* 0=sine, 1=rampdown, 2=square, 3=random (speed ignored) */
        };

        /* instrument information */

        static int ITENVCNT = 25;
        static int ITNOTECNT = 120;

        class ITNode
        {
            public byte flg;
            public byte pts;
            public byte beg;            /* (byte) Volume loop start (node) */
            public byte end;            /* (byte) Volume loop end (node) */
            public byte susbeg;     /* (byte) Volume sustain begin (node) */
            public byte susend;     /* (byte) Volume Sustain end (node) */
            public byte[] node = new byte[ITENVCNT];/* amplitude of volume nodes */
            public ushort[] tick = new ushort[ITENVCNT];   /* tick value of volume nodes */
        }

        class ITINSTHEADER
        {
            public ITINSTHEADER()
            {
                vol = new ITNode();
                pan = new ITNode();
                pit = new ITNode();
            }

            //public uint	size;			/* (dword) Instrument size */
            public string filename; /* (char) Instrument filename */
            public byte zerobyte;       /* (byte) Instrument type (always 0) */
            public ITNode vol;
            public ITNode pan;
            public ITNode pit;
            //public ushort	blank;
            public byte globvol;
            public byte chanpan;
            public ushort fadeout;      /* Envelope end / NNA volume fadeout */
            public byte dnc;            /* Duplicate note check */
            public byte dca;            /* Duplicate check action */
            public byte dct;            /* Duplicate check type */
            public byte nna;            /* New Note Action [0,1,2,3] */
            public ushort trkvers;      /* tracker version used to save [files only] */
            public byte ppsep;          /* Pitch-pan Separation */
            public byte ppcenter;       /* Pitch-pan Center */
            public byte rvolvar;        /* random volume varations */
            public byte rpanvar;        /* random panning varations */
            public ushort numsmp;           /* Number of samples in instrument [files only] */
            public string name;     /* Instrument name */
            public byte[] blank01 = new byte[6];
            public ushort[] samptable = new ushort[ITNOTECNT];/* sample for each note [note / samp pairs] */
            public byte[] volenv = new byte[200];        /* volume envelope (IT 1.x stuff) */
            public byte[] oldvoltick = new byte[ITENVCNT];/* volume tick position (IT 1.x stuff) */
        };

        /* unpacked note */

        class ITNOTE
        {
            public byte note, ins, volpan, cmd, inf;

            public void Clear()
            {
                note = ins = volpan = cmd = inf = 255;
            }
        };

        /*========== Loader data */

        static uint[]? paraptr = null;  /* parapointer array (see IT docs) */
        static ITHEADER? mh = null;
        static ITNOTE[]? itpat = null;  /* allocate to space for one full pattern */
        static byte[]? mask = null; /* arrays allocated to 64 elements and used for */
        static ITNOTE[]? last = null;   /* uncompressing IT's pattern information */
        static int numtrk = 0;
        static uint old_effect;     /* if set, use S3M old-effects stuffs */

        static string[] IT_Version ={
            "ImpulseTracker  .  ",
            "Compressed ImpulseTracker  .  ",
            "ImpulseTracker 2.14p3",
            "Compressed ImpulseTracker 2.14p3",
            "ImpulseTracker 2.14p4",
            "Compressed ImpulseTracker 2.14p4",
        };

        /* table for porta-to-note command within volume/panning column */
        static byte[] portatable = { 0, 1, 4, 8, 16, 32, 64, 96, 128, 255 };


        public ITLoader()
            : base()
        {
            m_ModuleType = "IT";
            m_ModuleVersion = "IT (Impulse Tracker)";
        }


        public override bool Test()
        {
            string id;
            id = m_Reader.Read_String(4);

            return id == "IMPM";
        }


        public override bool Init()
        {
            mh = new ITHEADER();
            poslookup = new byte[256];
            itpat = new ITNOTE[200 * 64];
            for (int i = 0; i < itpat.Length; i++)
            {
                itpat[i] = new ITNOTE();
            }

            mask = new byte[64];

            last = new ITNOTE[64];
            for (int i = 0; i < last.Length; i++)
            {
                last[i] = new ITNOTE();
            }

            return true;
        }


        /* Because so many IT files have 64 channels as the set number used, but really
		   only use far less (usually from 8 to 24 still), I had to make this function,
		   which determines the number of channels that are actually USED by a pattern.
 
		   NOTE: You must first seek to the file location of the pattern before calling
				 this procedure.

		   Returns 1 on error
		*/
        bool IT_GetNumChannels(ushort patrows)
        {
            int row = 0, flag, ch;

            do
            {
                flag = m_Reader.Read_byte();
                if (m_Reader.isEOF())
                {
                    m_LoadError = MMERR_LOADING_PATTERN;
                    return true;
                }
                if (flag == 0)
                    row++;
                else
                {
                    ch = (flag - 1) & 63;
                    remap[ch] = 0;
                    if ((flag & 128) != 0) mask[ch] = m_Reader.Read_byte();
                    if ((mask[ch] & 1) != 0) m_Reader.Read_byte();
                    if ((mask[ch] & 2) != 0) m_Reader.Read_byte();
                    if ((mask[ch] & 4) != 0) m_Reader.Read_byte();
                    if ((mask[ch] & 8) != 0) { m_Reader.Read_byte(); m_Reader.Read_byte(); }
                }
            } while (row < patrows);

            return false;
        }

        byte[] IT_ConvertTrack(ITNOTE[] tr, ushort numrows, int place)
        {
            int t;
            byte note, ins, volpan;

            UniReset();

            for (t = 0; t < numrows; t++)
            {
                note = tr[t * m_Module.numchn + place].note;
                ins = tr[t * m_Module.numchn + place].ins;
                volpan = tr[t * m_Module.numchn + place].volpan;

                if (note != 255)
                {
                    if (note == 253)
                        UniWriteByte(SharpMikCommon.Commands.UNI_KEYOFF);
                    else if (note == 254)
                    {
                        UniPTEffect(0xc, -1);   /* note cut command */
                        volpan = 255;
                    }
                    else
                        UniNote(note);
                }

                if ((ins != 0) && (ins < 100))
                    UniInstrument(ins - 1);
                else if (ins == 253)
                    UniWriteByte(SharpMikCommon.Commands.UNI_KEYOFF);
                else if (ins != 255)
                { /* crap */
                    m_LoadError = MMERR_LOADING_PATTERN;
                    return null;
                }

                /* process volume / panning column
				   volume / panning effects do NOT all share the same memory address
				   yet. */
                if (volpan <= 64)
                    UniVolEffect(SharpMikCommon.ITColumnEffect.VOL_VOLUME, volpan);
                else if (volpan == 65) /* fine volume slide up (65-74) - A0 case */
                    UniVolEffect(SharpMikCommon.ITColumnEffect.VOL_VOLSLIDE, 0);
                else if (volpan <= 74)
                { /* fine volume slide up (65-74) - general case */
                    UniVolEffect(SharpMikCommon.ITColumnEffect.VOL_VOLSLIDE, 0x0f + ((volpan - 65) << 4));
                }
                else if (volpan == 75)  /* fine volume slide down (75-84) - B0 case */
                    UniVolEffect(SharpMikCommon.ITColumnEffect.VOL_VOLSLIDE, 0);
                else if (volpan <= 84)
                {   /* fine volume slide down (75-84) - general case*/
                    UniVolEffect(SharpMikCommon.ITColumnEffect.VOL_VOLSLIDE, 0xf0 + (volpan - 75));
                }
                else if (volpan <= 94)  /* volume slide up (85-94) */
                    UniVolEffect(SharpMikCommon.ITColumnEffect.VOL_VOLSLIDE, ((volpan - 85) << 4));
                else if (volpan <= 104)/* volume slide down (95-104) */
                    UniVolEffect(SharpMikCommon.ITColumnEffect.VOL_VOLSLIDE, (volpan - 95));
                else if (volpan <= 114)/* pitch slide down (105-114) */
                    UniVolEffect(SharpMikCommon.ITColumnEffect.VOL_PITCHSLIDEDN, (volpan - 105));
                else if (volpan <= 124)/* pitch slide up (115-124) */
                    UniVolEffect(SharpMikCommon.ITColumnEffect.VOL_PITCHSLIDEUP, (volpan - 115));
                else if (volpan <= 127)
                { /* crap */
                    m_LoadError = MMERR_LOADING_PATTERN;
                    return null;
                }
                else if (volpan <= 192)
                    UniVolEffect(SharpMikCommon.ITColumnEffect.VOL_PANNING, ((volpan - 128) == 64) ? 255 : ((volpan - 128) << 2));
                else if (volpan <= 202)/* portamento to note */
                    UniVolEffect(SharpMikCommon.ITColumnEffect.VOL_PORTAMENTO, portatable[volpan - 193]);
                else if (volpan <= 212)/* vibrato */
                    UniVolEffect(SharpMikCommon.ITColumnEffect.VOL_VIBRATO, (volpan - 203));
                else if ((volpan != 239) && (volpan != 255))
                { /* crap */
                    m_LoadError = MMERR_LOADING_PATTERN;
                    return null;
                }

                S3MIT_ProcessCmd(tr[t * m_Module.numchn + place].cmd, tr[t * m_Module.numchn + place].inf, old_effect | SharpMikCommon.S3MIT_IT);

                UniNewline();
            }
            return UniDup();
        }

        bool IT_ReadPattern(ushort patrows)
        {
            int row = 0, flag, ch, blah;

            //ITNOTE *itt=itpat,dummy,*n,*l;

            int place = 0;
            ITNOTE dummy, n, l;
            dummy = new ITNOTE();

            for (int i = 0; i < 200 * 64; i++)
            {
                itpat[i].Clear();
            }

            do
            {
                flag = m_Reader.Read_byte();
                if (m_Reader.isEOF())
                {
                    m_LoadError = MMERR_LOADING_PATTERN;
                    return false;
                }

                if (flag == 0)
                {
                    place += m_Module.numchn;
                    row++;
                }
                else
                {
                    ch = remap[(flag - 1) & 63];
                    if (ch != -1)
                    {
                        n = itpat[ch + place];
                        l = last[ch];
                    }
                    else
                        n = l = dummy;

                    if ((flag & 128) != 0) mask[ch] = m_Reader.Read_byte();
                    if ((mask[ch] & 1) != 0)
                        /* convert IT note off to internal note off */
                        if ((l.note = n.note = m_Reader.Read_byte()) == 255)
                            l.note = n.note = 253;
                    if ((mask[ch] & 2) != 0)
                        l.ins = n.ins = m_Reader.Read_byte();
                    if ((mask[ch] & 4) != 0)
                        l.volpan = n.volpan = m_Reader.Read_byte();
                    if ((mask[ch] & 8) != 0)
                    {
                        l.cmd = n.cmd = m_Reader.Read_byte();
                        l.inf = n.inf = m_Reader.Read_byte();
                    }
                    if ((mask[ch] & 16) != 0)
                        n.note = l.note;
                    if ((mask[ch] & 32) != 0)
                        n.ins = l.ins;
                    if ((mask[ch] & 64) != 0)
                        n.volpan = l.volpan;
                    if ((mask[ch] & 128) != 0)
                    {
                        n.cmd = l.cmd;
                        n.inf = l.inf;
                    }
                }
            } while (row < patrows);


            // 			for (int i = 0; i < itpat.Length; i++)
            // 			{
            // 				Debug.WriteLine("{0} {1} {2} {3} {4}", itpat[i].cmd, itpat[i].inf, itpat[i].ins, itpat[i].note, itpat[i].volpan);
            // 		
            for (blah = 0; blah < m_Module.numchn; blah++)
            {
                if ((m_Module.tracks[numtrk++] = IT_ConvertTrack(itpat, patrows, blah)) == null)
                    return false;
            }

            return true;
        }

        void LoadMidiString(char[] dest)
        {
            m_Reader.Read_bytes(dest, 32);
            int cur;
            cur = 0;


            for (int i = 0; i < 32 && dest[i] != 0; i++)
            {
                if (char.IsNumber(dest[i]))
                {
                    dest[cur] = char.ToUpper(dest[i]);
                    cur++;
                }
            }
            dest[cur] = (char)0;
        }

        /* Load embedded midi information for resonant filters */
        void IT_LoadMidiConfiguration(bool readFile)
        {
            int i;
            filtermacros.Memset(0, filtermacros.Length);
            for (i = 0; i < filtersettings.Length; i++)
            {
                filtersettings[i].Clear();
            }

            if (readFile)
            { /* information is embedded in file */
                ushort dat;
                char[] midiline = new char[33];
                dat =
                dat = m_Reader.Read_Intel_ushort();
                m_Reader.Seek(8 * dat + 0x120, SeekOrigin.Current);

                /* read midi macros */
                for (i = 0; i < SharpMikCommon.UF_MAXMACRO; i++)
                {
                    LoadMidiString(midiline);
                    string test = new string(midiline);
                    if ((test.StartsWith("F0F00")) &&
                       ((midiline[5] == '0') || (midiline[5] == '1')))
                        filtermacros[i] = (byte)((midiline[5] - '0') | 0x80);
                }

                /* read standalone filters */
                for (i = 0x80; i < 0x100; i++)
                {
                    LoadMidiString(midiline);
                    string test = new string(midiline);
                    if ((test.StartsWith("F0F00")) &&
                       ((midiline[5] == '0') || (midiline[5] == '1')))
                    {
                        filtersettings[i].filter = (byte)((midiline[5] - '0') | 0x80);
                        dat = (ushort)((midiline[6] != 0) ? (midiline[6] - '0') : 0);
                        if (midiline[7] != 0) dat = (ushort)((dat << 4) | (midiline[7] - '0'));
                        filtersettings[i].inf = (byte)dat;
                    }
                }
            }
            else
            { /* use default information */
                filtermacros[0] = SharpMikCommon.FILT_CUT;
                for (i = 0x80; i < 0x90; i++)
                {
                    filtersettings[i].filter = SharpMikCommon.FILT_RESONANT;
                    filtersettings[i].inf = (byte)((i & 0x7f) << 3);
                }
            }
            activemacro = 0;
            for (i = 0; i < 0x80; i++)
            {
                filtersettings[i].filter = filtermacros[0];
                filtersettings[i].inf = (byte)i;
            }
        }


        void IT_LoadEnvelope(ITNode node, bool signed)
        {
            node.flg = m_Reader.Read_byte();
            node.pts = m_Reader.Read_byte();
            node.beg = m_Reader.Read_byte();
            node.end = m_Reader.Read_byte();
            node.susbeg = m_Reader.Read_byte();
            node.susend = m_Reader.Read_byte();

            for (int lp = 0; lp < ITENVCNT; lp++)
            {
                if (!signed)
                {
                    node.node[lp] = m_Reader.Read_byte();
                }
                else
                {
                    node.node[lp] = (byte)m_Reader.Read_sbyte();
                }

                node.tick[lp] = m_Reader.Read_Intel_ushort();
            }
            m_Reader.Read_byte();
        }

        void IT_ProcessEnvelopeVol(ITNode node, INSTRUMENT d)
        {
            if ((node.flg & 1) != 0) d.volflg |= SharpMikCommon.EF_ON;
            if ((node.flg & 2) != 0) d.volflg |= SharpMikCommon.EF_LOOP;
            if ((node.flg & 4) != 0) d.volflg |= SharpMikCommon.EF_SUSTAIN;

            d.volpts = node.pts;
            d.volbeg = node.beg;
            d.volend = node.end;
            d.volsusbeg = node.susbeg;
            d.volsusend = node.susend;

            for (int u = 0; u < node.pts; u++)
                d.volenv[u].pos = (short)node.tick[u];

            if ((d.volflg & SharpMikCommon.EF_ON) != 0 && (d.volpts < 2))
            {
                int flag = d.volflg;
                flag &= ~SharpMikCommon.EF_ON;
                d.volflg = (byte)flag;
            }
        }

        void IT_ProcessEnvelopePan(ITNode node, INSTRUMENT d)
        {
            if ((node.flg & 1) != 0) d.panflg |= SharpMikCommon.EF_ON;
            if ((node.flg & 2) != 0) d.panflg |= SharpMikCommon.EF_LOOP;
            if ((node.flg & 4) != 0) d.panflg |= SharpMikCommon.EF_SUSTAIN;

            d.panpts = node.pts;
            d.panbeg = node.beg;
            d.panend = node.end;
            d.pansusbeg = node.susbeg;
            d.pansusend = node.susend;

            for (int u = 0; u < node.pts; u++)
                d.panenv[u].pos = (short)node.tick[u];

            if ((d.panflg & SharpMikCommon.EF_ON) != 0 && (d.panpts < 2))
            {
                int flag = d.panflg;
                flag &= ~SharpMikCommon.EF_ON;
                d.panflg = (byte)flag;
            }
        }

        void IT_ProcessEnvelopePit(ITNode node, INSTRUMENT d)
        {
            if ((node.flg & 1) != 0) d.pitflg |= SharpMikCommon.EF_ON;
            if ((node.flg & 2) != 0) d.pitflg |= SharpMikCommon.EF_LOOP;
            if ((node.flg & 4) != 0) d.pitflg |= SharpMikCommon.EF_SUSTAIN;

            d.pitpts = node.pts;
            d.pitbeg = node.beg;
            d.pitend = node.end;
            d.pitsusbeg = node.susbeg;
            d.pitsusend = node.susend;

            for (int u = 0; u < node.pts; u++)
                d.pitenv[u].pos = (short)node.tick[u];

            if ((d.pitflg & SharpMikCommon.EF_ON) != 0 && (d.pitpts < 2))
            {
                int flag = d.pitflg;
                flag &= ~SharpMikCommon.EF_ON;
                d.pitflg = (byte)flag;
            }
        }

        public override bool Load(int curious)
        {
            int t, u, lp;
            INSTRUMENT d;
            SAMPLE q;

            numtrk = 0;
            filters = false;

            /* try to read module header */
            m_Reader.Read_Intel_uint(); /* kill the 4 byte header */
            mh.songname = m_Reader.Read_String(26);
            m_Reader.Read_bytes(mh.blank01, 2);
            mh.ordnum = m_Reader.Read_Intel_ushort();
            mh.insnum = m_Reader.Read_Intel_ushort();
            mh.smpnum = m_Reader.Read_Intel_ushort();
            mh.patnum = m_Reader.Read_Intel_ushort();
            mh.cwt = m_Reader.Read_Intel_ushort();
            mh.cmwt = m_Reader.Read_Intel_ushort();
            mh.flags = m_Reader.Read_Intel_ushort();
            mh.special = m_Reader.Read_Intel_ushort();
            mh.globvol = m_Reader.Read_byte();
            mh.mixvol = m_Reader.Read_byte();
            mh.initspeed = m_Reader.Read_byte();
            mh.inittempo = m_Reader.Read_byte();
            mh.pansep = m_Reader.Read_byte();
            mh.zerobyte = m_Reader.Read_byte();
            mh.msglength = m_Reader.Read_Intel_ushort();
            mh.msgoffset = m_Reader.Read_Intel_uint();
            m_Reader.Read_bytes(mh.blank02, 4);
            m_Reader.Read_bytes(mh.pantable, 64);
            m_Reader.Read_bytes(mh.voltable, 64);

            if (m_Reader.isEOF())
            {
                m_LoadError = MMERR_LOADING_HEADER;
                return false;
            }

            /* set module variables */
            m_Module.songname = mh.songname; /* make a cstr of songname  */
            m_Module.reppos = 0;
            m_Module.numpat = mh.patnum;
            m_Module.numins = mh.insnum;
            m_Module.numsmp = mh.smpnum;
            m_Module.initspeed = mh.initspeed;
            m_Module.inittempo = mh.inittempo;
            m_Module.initvolume = mh.globvol;
            m_Module.flags |= SharpMikCommon.UF_BGSLIDES | SharpMikCommon.UF_ARPMEM;

            if ((mh.flags & 1) == 0)
                m_Module.flags |= SharpMikCommon.UF_PANNING;
            m_Module.bpmlimit = 32;

            if (mh.songname.Length > 25 && mh.songname[25] != 0) // Embedded IT limitation
            {
                m_Module.numvoices = (byte)(1 + mh.songname[25]);
            }

            /* set the module type */
            /* 2.17 : IT 2.14p4 */
            /* 2.16 : IT 2.14p3 with resonant filters */
            /* 2.15 : IT 2.14p3 (improved compression) */
            if ((mh.cwt <= 0x219) && (mh.cwt >= 0x217))
                m_Module.modtype = IT_Version[mh.cmwt < 0x214 ? 4 : 5];
            else if (mh.cwt >= 0x215)
                m_Module.modtype = IT_Version[mh.cmwt < 0x214 ? 2 : 3];
            else
            {
                char[] modType = IT_Version[mh.cmwt < 0x214 ? 0 : 1].ToCharArray();

                modType[mh.cmwt < 0x214 ? 15 : 26] = (char)((mh.cwt >> 8) + '0');
                modType[mh.cmwt < 0x214 ? 17 : 28] = (char)(((mh.cwt >> 4) & 0xf) + '0');
                modType[mh.cmwt < 0x214 ? 18 : 29] = (char)(((mh.cwt) & 0xf) + '0');
                m_Module.modtype = new string(modType);
            }

            if ((mh.flags & 8) != 0)
                m_Module.flags |= SharpMikCommon.UF_XMPERIODS | SharpMikCommon.UF_LINEAR;

            if ((mh.cwt >= 0x106) && (mh.flags & 16) != 0)
                old_effect = SharpMikCommon.S3MIT_OLDSTYLE;
            else
                old_effect = 0;

            /* set panning positions */
            if ((mh.flags & 1) != 0)
                for (t = 0; t < 64; t++)
                {
                    mh.pantable[t] &= 0x7f;
                    if (mh.pantable[t] < 64)
                        m_Module.panning[t] = (ushort)(mh.pantable[t] << 2);
                    else if (mh.pantable[t] == 64)
                        m_Module.panning[t] = 255;
                    else if (mh.pantable[t] == 100)
                        m_Module.panning[t] = SharpMikCommon.PAN_SURROUND;
                    else if (mh.pantable[t] == 127)
                        m_Module.panning[t] = SharpMikCommon.PAN_CENTER;
                    else
                    {
                        m_LoadError = MMERR_LOADING_HEADER;
                        return false;
                    }
                }
            else
                for (t = 0; t < 64; t++)
                    m_Module.panning[t] = SharpMikCommon.PAN_CENTER;

            /* set channel volumes */
            for (int j = 0; j < 64; j++)
            {
                m_Module.chanvol[j] = mh.voltable[j];
            }

            int test = m_Reader.Tell();

            /* read the order data */
            m_Module.positions = new ushort[mh.ordnum];
            origpositions = new ushort[mh.ordnum];

            for (t = 0; t < mh.ordnum; t++)
            {
                origpositions[t] = m_Reader.Read_byte();
                if ((origpositions[t] > mh.patnum) && (origpositions[t] < 254))
                    origpositions[t] = 255;
            }

            if (m_Reader.isEOF())
            {
                m_LoadError = MMERR_LOADING_HEADER;
                return false;
            }

            poslookupcnt = mh.ordnum;
            S3MIT_CreateOrders(curious);

            paraptr = new uint[mh.insnum + mh.smpnum + m_Module.numpat];

            /* read the instrument, sample, and pattern parapointers */
            m_Reader.Read_Intel_uints(paraptr, mh.insnum + mh.smpnum + m_Module.numpat);

            if (m_Reader.isEOF())
            {
                m_LoadError = MMERR_LOADING_HEADER;
                return false;
            }

            /* Check for and load midi information for resonant filters */
            if (mh.cmwt >= 0x216)
            {
                if ((mh.special & 8) != 0)
                {
                    IT_LoadMidiConfiguration(true);
                    if (m_Reader.isEOF())
                    {
                        m_LoadError = MMERR_LOADING_HEADER;
                        return false;
                    }
                }
                else
                    IT_LoadMidiConfiguration(false);
                filters = true;
            }

            /* Check for and load song comment */
            if ((mh.special & 1) != 0 && (mh.cwt >= 0x104) && (mh.msglength != 0))
            {
                m_Reader.Seek((int)(mh.msgoffset), SeekOrigin.Begin);

                if (!ReadComment(mh.msglength))
                    return false;
            }

            if ((mh.flags & 4) == 0) m_Module.numins = m_Module.numsmp;
            m_Module.AllocSamples();
            AllocLinear();


            /* Load all samples */

            for (t = 0; t < mh.smpnum; t++)
            {
                q = m_Module.samples[t];
                ITSAMPLE s = new ITSAMPLE();

                /* seek to sample position */
                m_Reader.Seek((int)(paraptr[mh.insnum + t] + 4), SeekOrigin.Begin);

                /* load sample info */

                s.filename = m_Reader.Read_String(12);
                s.zerobyte = m_Reader.Read_byte();
                s.globvol = m_Reader.Read_byte();
                s.flag = m_Reader.Read_byte();
                s.volume = m_Reader.Read_byte();
                s.sampname = m_Reader.Read_String(26);
                s.convert = m_Reader.Read_byte();
                s.panning = m_Reader.Read_byte();
                s.length = m_Reader.Read_Intel_uint();
                s.loopbeg = m_Reader.Read_Intel_uint();
                s.loopend = m_Reader.Read_Intel_uint();
                s.c5spd = m_Reader.Read_Intel_uint();
                s.susbegin = m_Reader.Read_Intel_uint();
                s.susend = m_Reader.Read_Intel_uint();
                s.sampoffset = m_Reader.Read_Intel_uint();
                s.vibspeed = m_Reader.Read_byte();
                s.vibdepth = m_Reader.Read_byte();
                s.vibrate = m_Reader.Read_byte();
                s.vibwave = m_Reader.Read_byte();

                /* Generate an error if c5spd is > 512k, or samplelength > 256 megs
				   (nothing would EVER be that high) */

                if (m_Reader.isEOF() || (s.c5spd > 0x7ffffL) || (s.length > 0xfffffffUL))
                {
                    m_LoadError = MMERR_LOADING_SAMPLEINFO;
                    return false;
                }

                /* Reality check for sample loop information */
                if ((s.flag & 16) != 0 &&
                   ((s.loopbeg > 0xfffffffUL) || (s.loopend > 0xfffffffUL)))
                {
                    m_LoadError = MMERR_LOADING_SAMPLEINFO;
                    return false;
                }

                q.samplename = s.sampname;
                q.speed = s.c5spd / 2;
                q.panning = (short)(((s.panning & 127) == 64) ? 255 : (s.panning & 127) << 2);
                q.length = s.length;
                q.loopstart = s.loopbeg;
                q.loopend = s.loopend;
                q.volume = s.volume;
                q.globvol = s.globvol;
                q.seekpos = s.sampoffset;

                /* Convert speed to XM linear finetune */
                if ((m_Module.flags & SharpMikCommon.UF_LINEAR) != 0)
                    q.speed = (uint)speed_to_finetune(s.c5spd, t);

                if ((s.panning & 128) != 0) q.flags |= SharpMikCommon.SF_OWNPAN;

                if (s.vibrate != 0)
                {
                    q.vibflags |= SharpMikCommon.AV_IT;
                    q.vibtype = s.vibwave;
                    q.vibsweep = (byte)(s.vibrate * 2);
                    q.vibdepth = s.vibdepth;
                    q.vibrate = s.vibspeed;
                }

                if ((s.flag & 2) != 0)
                    q.flags |= SharpMikCommon.SF_16BITS;

                if ((s.flag & 8) != 0 && (mh.cwt >= 0x214))
                {
                    q.flags |= SharpMikCommon.SF_ITPACKED;
                }

                if ((s.flag & 16) != 0) q.flags |= SharpMikCommon.SF_LOOP;
                if ((s.flag & 64) != 0) q.flags |= SharpMikCommon.SF_BIDI;

                if (mh.cwt >= 0x200)
                {
                    if ((s.convert & 1) != 0) q.flags |= SharpMikCommon.SF_SIGNED;
                    if ((s.convert & 4) != 0) q.flags |= SharpMikCommon.SF_DELTA;
                }
            }

            /* Load instruments if instrument mode flag enabled */

            if ((mh.flags & 4) != 0)
            {
                if (!m_Module.AllocInstruments()) return false;

                m_Module.flags |= SharpMikCommon.UF_NNA | SharpMikCommon.UF_INST;

                for (t = 0; t < mh.insnum; t++)
                {
                    d = m_Module.instruments[t];
                    ITINSTHEADER ih = new ITINSTHEADER();

                    /* seek to instrument position */
                    m_Reader.Seek((int)(paraptr[t] + 4), SeekOrigin.Begin);
                    /* load instrument info */
                    ih.filename = m_Reader.Read_String(12);
                    ih.zerobyte = m_Reader.Read_byte();
                    if (mh.cwt < 0x200)
                    {
                        /* load IT 1.xx inst header */
                        ih.vol.flg = m_Reader.Read_byte();
                        ih.vol.beg = m_Reader.Read_byte();
                        ih.vol.end = m_Reader.Read_byte();
                        ih.vol.susbeg = m_Reader.Read_byte();
                        ih.vol.susend = m_Reader.Read_byte();
                        m_Reader.Read_Intel_ushort();
                        ih.fadeout = m_Reader.Read_Intel_ushort();
                        ih.nna = m_Reader.Read_byte();
                        ih.dnc = m_Reader.Read_byte();
                    }
                    else
                    {
                        /* Read IT200+ header */
                        ih.nna = m_Reader.Read_byte();
                        ih.dct = m_Reader.Read_byte();
                        ih.dca = m_Reader.Read_byte();
                        ih.fadeout = m_Reader.Read_Intel_ushort();
                        ih.ppsep = m_Reader.Read_byte();
                        ih.ppcenter = m_Reader.Read_byte();
                        ih.globvol = m_Reader.Read_byte();
                        ih.chanpan = m_Reader.Read_byte();
                        ih.rvolvar = m_Reader.Read_byte();
                        ih.rpanvar = m_Reader.Read_byte();
                    }

                    ih.trkvers = m_Reader.Read_Intel_ushort();
                    ih.numsmp = m_Reader.Read_byte();
                    m_Reader.Read_byte();
                    ih.name = m_Reader.Read_String(26);
                    m_Reader.Read_bytes(ih.blank01, 6);
                    m_Reader.Read_Intel_ushorts(ih.samptable, ITNOTECNT);

                    if (mh.cwt < 0x200)
                    {
                        /* load IT 1xx volume envelope */
                        m_Reader.Read_bytes(ih.volenv, 200);
                        for (lp = 0; lp < ITENVCNT; lp++)
                        {
                            ih.oldvoltick[lp] = m_Reader.Read_byte();
                            ih.vol.node[lp] = m_Reader.Read_byte();
                        }
                    }
                    else
                    {
                        /* load IT 2xx volume, pan and pitch envelopes */
                        IT_LoadEnvelope(ih.vol, false);
                        IT_LoadEnvelope(ih.pan, true);
                        IT_LoadEnvelope(ih.pit, true);
                    }

                    if (m_Reader.isEOF())
                    {
                        m_LoadError = MMERR_LOADING_SAMPLEINFO;
                        return false;
                    }

                    d.volflg |= SharpMikCommon.EF_VOLENV;
                    d.insname = ih.name;
                    d.nnatype = (byte)(ih.nna & SharpMikCommon.NNA_MASK);

                    if (mh.cwt < 0x200)
                    {
                        d.volfade = (ushort)(ih.fadeout << 6);
                        if (ih.dnc != 0)
                        {
                            d.dct = SharpMikCommon.DCT_NOTE;
                            d.dca = SharpMikCommon.DCA_CUT;
                        }

                        if ((ih.vol.flg & 1) != 0) d.volflg |= SharpMikCommon.EF_ON;
                        if ((ih.vol.flg & 2) != 0) d.volflg |= SharpMikCommon.EF_LOOP;
                        if ((ih.vol.flg & 4) != 0) d.volflg |= SharpMikCommon.EF_SUSTAIN;

                        /* XM conversion of IT envelope Array */
                        d.volbeg = ih.vol.beg;
                        d.volend = ih.vol.end;
                        d.volsusbeg = ih.vol.susbeg;
                        d.volsusend = ih.vol.susend;

                        if ((ih.vol.flg & 1) != 0)
                        {
                            for (u = 0; u < ITENVCNT; u++)
                                if (ih.oldvoltick[d.volpts] != 0xff)
                                {
                                    d.volenv[d.volpts].val = (short)((ih.vol.node[d.volpts] << 2));
                                    d.volenv[d.volpts].pos = ih.oldvoltick[d.volpts];
                                    d.volpts++;
                                }
                                else
                                    break;
                        }
                    }
                    else
                    {
                        d.panning = (short)(((ih.chanpan & 127) == 64) ? 255 : (ih.chanpan & 127) << 2);
                        if ((ih.chanpan & 128) == 0) d.flags |= SharpMikCommon.IF_OWNPAN;

                        if ((ih.ppsep & 128) == 0)
                        {
                            d.pitpansep = (byte)(ih.ppsep << 2);
                            d.pitpancenter = ih.ppcenter;
                            d.flags |= SharpMikCommon.IF_PITCHPAN;
                        }
                        d.globvol = (byte)(ih.globvol >> 1);
                        d.volfade = (ushort)(ih.fadeout << 5);
                        d.dct = ih.dct;
                        d.dca = ih.dca;

                        if (mh.cwt >= 0x204)
                        {
                            d.rvolvar = ih.rvolvar;
                            d.rpanvar = ih.rpanvar;
                        }



                        IT_ProcessEnvelopeVol(ih.vol, d);
                        for (u = 0; u < ih.vol.pts; u++)
                            d.volenv[u].val = (short)(ih.vol.node[u] << 2);

                        IT_ProcessEnvelopePan(ih.pan, d);
                        for (u = 0; u < ih.pan.pts; u++)
                        {
                            sbyte pan = (sbyte)ih.pan.node[u];
                            d.panenv[u].val = (short)(pan == 32 ? 255 : (pan + 32) << 2);
                        }

                        IT_ProcessEnvelopePit(ih.pit, d);
                        for (u = 0; u < ih.pit.pts; u++)
                        {
                            sbyte pit = (sbyte)ih.pit.node[u];
                            d.pitenv[u].val = (short)(pit + 32);
                        }


                        if ((ih.pit.flg & 0x80) != 0)
                        {
                            /* filter envelopes not supported yet */
                            int flag = d.pitflg;
                            flag &= ~SharpMikCommon.EF_ON;
                            d.pitflg = (byte)flag;
                            ih.pit.pts = ih.pit.beg = ih.pit.end = 0;
                        }
                    }

                    for (u = 0; u < ITNOTECNT; u++)
                    {
                        d.samplenote[u] = (byte)(ih.samptable[u] & 255);
                        d.samplenumber[u] = (ushort)((ih.samptable[u] >> 8 != 0) ? ((ih.samptable[u] >> 8) - 1) : 0xffff);
                        if (d.samplenumber[u] >= m_Module.numsmp)
                            d.samplenote[u] = 255;
                        else if ((m_Module.flags & SharpMikCommon.UF_LINEAR) != 0)
                        {
                            int note = (int)d.samplenote[u] + noteindex[d.samplenumber[u]];
                            d.samplenote[u] = (byte)((note < 0) ? 0 : (note > 255 ? 255 : note));
                        }
                    }
                }
            }
            else if ((m_Module.flags & SharpMikCommon.UF_LINEAR) != 0)
            {
                if (!m_Module.AllocInstruments()) return false;

                m_Module.flags |= SharpMikCommon.UF_INST;

                for (t = 0; t < mh.smpnum; t++)
                {
                    d = m_Module.instruments[t];
                    for (u = 0; u < ITNOTECNT; u++)
                    {
                        if (d.samplenumber[u] >= m_Module.numsmp)
                            d.samplenote[u] = 255;
                        else
                        {
                            int note = (int)d.samplenote[u] + noteindex[d.samplenumber[u]];
                            d.samplenote[u] = (byte)((note < 0) ? 0 : (note > 255 ? 255 : note));
                        }
                    }
                }
            }

            /* Figure out how many channels this song actually uses */
            m_Module.numchn = 0;
            remap.Memset(-1, SharpMikCommon.UF_MAXCHAN);

            for (t = 0; t < m_Module.numpat; t++)
            {
                ushort packlen;

                /* seek to pattern position */
                if (paraptr[mh.insnum + mh.smpnum + t] != 0)  /* 0 . empty 64 row pattern */
                {
                    m_Reader.Seek((int)paraptr[mh.insnum + mh.smpnum + t], SeekOrigin.Begin);
                    m_Reader.Read_Intel_ushort();
                    /* read pattern length (# of rows)
					   Impulse Tracker never creates patterns with less than 32 rows,
					   but some other trackers do, so we only check for more than 256
					   rows */
                    packlen = m_Reader.Read_Intel_ushort();
                    if (packlen > 256)
                    {
                        m_LoadError = MMERR_LOADING_PATTERN;
                        return false;
                    }
                    m_Reader.Read_Intel_uint();
                    if (IT_GetNumChannels(packlen)) return false;
                }
            }

            /* give each of them a different number */
            for (t = 0; t < SharpMikCommon.UF_MAXCHAN; t++)
            {
                if (remap[t] == 0)
                    remap[t] = (sbyte)(m_Module.numchn++);
            }
            m_Module.numtrk = (ushort)(m_Module.numpat * m_Module.numchn);
            if (m_Module.numvoices != 0)
                if (m_Module.numvoices < m_Module.numchn) m_Module.numvoices = m_Module.numchn;

            m_Module.AllocPatterns();
            m_Module.AllocTracks();

            for (t = 0; t < m_Module.numpat; t++)
            {
                ushort packlen;

                /* seek to pattern position */
                if (paraptr[mh.insnum + mh.smpnum + t] == 0)
                { /* 0 . empty 64 row pattern */
                    m_Module.pattrows[t] = 64;
                    for (u = 0; u < m_Module.numchn; u++)
                    {
                        int k;

                        UniReset();
                        for (k = 0; k < 64; k++) UniNewline();
                        m_Module.tracks[numtrk++] = UniDup();
                    }
                }
                else
                {
                    m_Reader.Seek((int)paraptr[mh.insnum + mh.smpnum + t], SeekOrigin.Begin);
                    packlen = m_Reader.Read_Intel_ushort();
                    m_Module.pattrows[t] = m_Reader.Read_Intel_ushort();
                    m_Reader.Read_Intel_uint();
                    if (!IT_ReadPattern(m_Module.pattrows[t])) return false;
                }
            }

            return true;
        }

        public override void Cleanup()
        {
            mh = null;
            poslookup = null;
            itpat = null;
            mask = null;
            last = null;
        }

        public override string LoadTitle()
        {
            throw new NotImplementedException();
        }
    }
}

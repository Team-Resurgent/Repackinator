using SharpMik.Attributes;
using SharpMik.Interfaces;

namespace SharpMik.Loaders
{
    [ModFileExtentions(".far")]
    public class FARLoader : IModLoader
    {
        /*========== Module structure */

        class FARHEADER1
        {
            public byte[] id = new byte[4];         /* file magic */
            public string songname;     /* songname */
            public char[] blah = new char[3];           /* 13,10,26 */
            public ushort headerlen;        /* remaining length of header in bytes */
            public byte version;
            public byte[] onoff = new byte[16];
            public byte[] edit1 = new byte[9];
            public byte speed;
            public byte[] panning = new byte[16];
            public byte[] edit2 = new byte[4];
            public ushort stlen;
        };

        class FARHEADER2
        {
            public byte[] orders = new byte[256];
            public byte numpat;
            public byte snglen;
            public byte loopto;
            public ushort[] patsiz = new ushort[256];
        };

        class FARSAMPLE
        {
            public string samplename;
            public uint length;
            public byte finetune;
            public byte volume;
            public uint reppos;
            public uint repend;
            public byte type;
            public byte loop;
        };

        class FARNOTE
        {
            public byte note, ins, vol, eff;

            public void Clear()
            {
                note = ins = vol = eff = 0;
            }
        };

        /*========== Loader variables */

        static string FAR_Version = "Farandole";
        static FARHEADER1? mh1 = null;
        static FARHEADER2? mh2 = null;
        static FARNOTE[]? pat = null;

        static char[] FARSIG = { 'F', 'A', 'R', (char)0xfe, (char)13, (char)10, (char)26 };
        static string FARSig1 = "FAR";
        static string FARSig2 = new string(new char[] { (char)13, (char)10, (char)26 });



        public FARLoader()
            : base()
        {
            m_ModuleType = "FAR";
            m_ModuleVersion = "FAR (Farandole Composer)";
        }

        public override bool Test()
        {
            string id = m_Reader.Read_String(47);

            return id.StartsWith(FARSig1) || id.EndsWith(FARSig2);
        }

        public override bool Init()
        {
            mh1 = new FARHEADER1();
            mh2 = new FARHEADER2();

            pat = new FARNOTE[256 * 16 * 4];

            for (int i = 0; i < pat.Length; i++)
            {
                pat[i] = new FARNOTE();
            }

            return true;
        }


        byte[] FAR_ConvertTrack(FARNOTE[] notes, int rows, int place)
        {
            int t, vibdepth = 1;

            UniReset();
            for (t = 0; t < rows; t++)
            {
                FARNOTE n = notes[place];
                if (n.note != 0)
                {
                    UniInstrument(n.ins);
                    UniNote(n.note + 3 * SharpMikCommon.Octave - 1);
                }
                if ((n.vol & 0xf) != 0)
                    UniPTEffect(0xc, (n.vol & 0xf) << 2);
                if (n.eff != 0)
                    switch (n.eff >> 4)
                    {
                        case 0x3: /* porta to note */
                            UniPTEffect(0x3, (n.eff & 0xf) << 4);
                            break;
                        case 0x4: /* retrigger */
                            UniPTEffect(0x0e, 0x90 | (n.eff & 0x0f));
                            break;
                        case 0x5: /* set vibrato depth */
                            vibdepth = n.eff & 0xf;
                            break;
                        case 0x6: /* vibrato */
                            UniPTEffect(0x4, ((n.eff & 0xf) << 4) | vibdepth);
                            break;
                        case 0x7: /* volume slide up */
                            UniPTEffect(0xa, (n.eff & 0xf) << 4);
                            break;
                        case 0x8: /* volume slide down */
                            UniPTEffect(0xa, n.eff & 0xf);
                            break;
                        case 0xb: /* panning */
                            UniPTEffect(0xe, 0x80 | (n.eff & 0xf));
                            break;
                        case 0xf: /* set speed */
                            UniPTEffect(0xf, n.eff & 0xf);
                            break;

                        /* others not yet implemented */
                        default:
                            break;
                    }

                UniNewline();
                place += 16;
            }
            return UniDup();
        }


        public override bool Load(int curious)
        {
            int t, u, tracks = 0;
            SAMPLE q;
            FARSAMPLE s;

            byte[] smap = new byte[8];

            /* try to read module header (first part) */
            m_Reader.Read_bytes(mh1.id, 4);
            mh1.songname = m_Reader.Read_String(40);
            m_Reader.Read_bytes(mh1.blah, 3);
            mh1.headerlen = m_Reader.Read_Intel_ushort();
            mh1.version = m_Reader.Read_byte();
            m_Reader.Read_bytes(mh1.onoff, 16);
            m_Reader.Read_bytes(mh1.edit1, 9);
            mh1.speed = m_Reader.Read_byte();
            m_Reader.Read_bytes(mh1.panning, 16);
            m_Reader.Read_bytes(mh1.edit2, 4);
            mh1.stlen = m_Reader.Read_Intel_ushort();

            /* init modfile data */
            m_Module.modtype = FAR_Version;
            m_Module.songname = mh1.songname;
            m_Module.numchn = 16;
            m_Module.initspeed = mh1.speed;
            m_Module.inittempo = 80;
            m_Module.reppos = 0;
            m_Module.flags |= SharpMikCommon.UF_PANNING;
            for (t = 0; t < 16; t++)
                m_Module.panning[t] = (ushort)(mh1.panning[t] << 4);

            /* read songtext into comment field */
            if (mh1.stlen != 0)
                if (!ReadLinedComment(mh1.stlen, 66))
                    return false;

            /* try to read module header (second part) */
            m_Reader.Read_bytes(mh2.orders, 256);
            mh2.numpat = m_Reader.Read_byte();
            mh2.snglen = m_Reader.Read_byte();
            mh2.loopto = m_Reader.Read_byte();
            m_Reader.Read_Intel_ushorts(mh2.patsiz, 256);

            m_Module.numpos = mh2.snglen;
            m_Module.positions = new ushort[m_Module.numpos];

            for (t = 0; t < m_Module.numpos; t++)
            {
                if (mh2.orders[t] == 0xff) break;
                m_Module.positions[t] = mh2.orders[t];
            }

            /* count number of patterns stored in file */
            m_Module.numpat = 0;
            for (t = 0; t < 256; t++)
                if (mh2.patsiz[t] != 0)
                    if ((t + 1) > m_Module.numpat)
                        m_Module.numpat = (ushort)(t + 1);
            m_Module.numtrk = (ushort)(m_Module.numpat * m_Module.numchn);

            /* seek across eventual new data */
            m_Reader.Seek(mh1.headerlen - (869 + mh1.stlen), SeekOrigin.Current);

            /* alloc track and pattern structures */
            m_Module.AllocTracks();
            m_Module.AllocPatterns();

            for (t = 0; t < m_Module.numpat; t++)
            {
                byte rows = 0, tempo;

                for (int i = 0; i < pat.Length; i++)
                {
                    pat[i].Clear();
                }

                if (mh2.patsiz[t] != 0)
                {
                    rows = m_Reader.Read_byte();
                    tempo = m_Reader.Read_byte();


                    /* file often allocates 64 rows even if there are less in pattern */
                    if (mh2.patsiz[t] < 2 + (rows * 16 * 4))
                    {
                        m_LoadError = MMERR_LOADING_PATTERN;
                        return false;
                    }

                    int place = 0;
                    for (u = (mh2.patsiz[t] - 2) / 4; u != 0; u--)
                    {
                        FARNOTE crow = pat[place];

                        crow.note = m_Reader.Read_byte();
                        crow.ins = m_Reader.Read_byte();
                        crow.vol = m_Reader.Read_byte();
                        crow.eff = m_Reader.Read_byte();

                        place++;
                    }

                    if (m_Reader.isEOF())
                    {
                        m_LoadError = MMERR_LOADING_PATTERN;
                        return false;
                    }


                    m_Module.pattrows[t] = rows;
                    place = 0;
                    for (u = 16; u != 0; u--)
                    {
                        if ((m_Module.tracks[tracks++] = FAR_ConvertTrack(pat, rows, place)) == null)
                        {
                            m_LoadError = MMERR_LOADING_PATTERN;
                            return false;
                        }

                        place++;
                    }
                }
                else
                    tracks += 16;
            }

            /* read sample map */
            m_Reader.Read_bytes(smap, 8);


            /* count number of samples used */
            m_Module.numins = 0;
            for (t = 0; t < 64; t++)
                if ((smap[t >> 3] & (1 << (t & 7))) != 0)
                    m_Module.numins = (ushort)(t + 1);
            m_Module.numsmp = m_Module.numins;

            /* alloc sample structs */
            m_Module.AllocSamples();


            for (t = 0; t < m_Module.numsmp; t++)
            {
                s = new FARSAMPLE();
                q = m_Module.samples[t];
                q.speed = 8363;
                q.flags = SharpMikCommon.SF_SIGNED;

                if ((smap[t >> 3] & (1 << (t & 7))) != 0)
                {
                    s.samplename = m_Reader.Read_String(32);

                    s.length = m_Reader.Read_Intel_uint();
                    s.finetune = m_Reader.Read_byte();
                    s.volume = m_Reader.Read_byte();
                    s.reppos = m_Reader.Read_Intel_uint();
                    s.repend = m_Reader.Read_Intel_uint();
                    s.type = m_Reader.Read_byte();
                    s.loop = m_Reader.Read_byte();

                    q.samplename = s.samplename;
                    q.length = s.length;
                    q.loopstart = s.reppos;
                    q.loopend = s.repend;
                    q.volume = (byte)(s.volume << 2);

                    if ((s.type & 1) != 0) q.flags |= SharpMikCommon.SF_16BITS;
                    if ((s.loop & 8) != 0) q.flags |= SharpMikCommon.SF_LOOP;

                    q.seekpos = (uint)m_Reader.Tell();
                    m_Reader.Seek((int)q.length, SeekOrigin.Current);
                }
                else
                    q.samplename = null;
            }

            return true;
        }

        public override void Cleanup()
        {
            mh1 = null;
            mh2 = null;

            pat = null;
        }

        public override string LoadTitle()
        {
            throw new NotImplementedException();
        }
    }
}

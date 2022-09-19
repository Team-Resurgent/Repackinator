using SharpMik.Attributes;
using SharpMik.Interfaces;

namespace SharpMik.Loaders
{
    [ModFileExtentions(".ult")]
    public class ULTLoader : IModLoader
    {
        struct ULTHEADER
        {
            public string id;
            public string songtitle;
            public byte reserved;
        };

        /* sample information */
        struct ULTSAMPLE
        {
            public string samplename;
            public string dosname;
            public int loopstart;
            public int loopend;
            public int sizestart;
            public int sizeend;
            public byte volume;
            public byte flags;
            public ushort speed;
            public short finetune;
        };

        struct ULTEVENT
        {
            public byte note, sample, eff, dat1, dat2;
        }

        static int ULTS_16BITS = 4;
        static int ULTS_LOOP = 8;

        static string ULT_Version = "Ultra Tracker v1.x";

        static ULTEVENT ev;


        public ULTLoader()
        {
            m_ModuleType = "ULT";
            m_ModuleVersion = "ULT (UltraTracker)";
        }

        public override bool Init()
        {
            return true;
        }

        public override bool Test()
        {
            string id = m_Reader.Read_String(15);

            if (id.StartsWith("MAS_UTrack_V00"))
            {
                if (id.Length < 15 || id[14] < '1' || id[14] > '4')
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }


        byte ReadUltEvent(ref ULTEVENT uEvent)
        {
            byte flag, rep = 1;
            flag = m_Reader.Read_byte();

            if (flag == 0xfc)
            {
                rep = m_Reader.Read_byte();
                uEvent.note = m_Reader.Read_byte();
            }
            else
                uEvent.note = flag;

            uEvent.sample = m_Reader.Read_byte();
            uEvent.eff = m_Reader.Read_byte();
            uEvent.dat1 = m_Reader.Read_byte();
            uEvent.dat2 = m_Reader.Read_byte();

            return rep;
        }


        public override bool Load(int curious)
        {
            int t, u, tracks = 0;
            SAMPLE q;
            ULTSAMPLE s;
            ULTHEADER mh;
            byte nos, noc, nop;

            /* try to read module header */
            mh.id = m_Reader.Read_String(15);
            mh.songtitle = m_Reader.Read_String(32);
            mh.reserved = m_Reader.Read_byte();

            if (m_Reader.isEOF())
            {
                m_LoadError = "MMERR_LOADING_HEADER";
                return false;
            }

            char[] ver = ULT_Version.ToCharArray();
            ver[ULT_Version.Length - 1] = (char)('3' + (mh.id[14] - '1'));
            m_Module.modtype = new string(ver);

            m_Module.initspeed = 6;
            m_Module.inittempo = 125;
            m_Module.reppos = 0;

            /* read songtext */
            if ((mh.id[14] > '1') && (mh.reserved != 0))
                if (!ReadLinedComment((ushort)(mh.reserved * 32), (ushort)32))
                    return false;

            nos = m_Reader.Read_byte();
            if (m_Reader.isEOF())
            {
                m_LoadError = "MMERR_LOADING_HEADER";
                return false;
            }

            m_Module.songname = mh.songtitle;
            m_Module.numins = m_Module.numsmp = nos;

            m_Module.AllocSamples();



            for (t = 0; t < nos; t++)
            {
                s = new ULTSAMPLE();
                q = m_Module.samples[t];
                /* try to read sample info */
                s.samplename = m_Reader.Read_String(32);
                s.dosname = m_Reader.Read_String(12);
                s.loopstart = m_Reader.Read_Intel_int();
                s.loopend = m_Reader.Read_Intel_int();
                s.sizestart = m_Reader.Read_Intel_int();
                s.sizeend = m_Reader.Read_Intel_int();
                s.volume = m_Reader.Read_byte();
                s.flags = m_Reader.Read_byte();
                s.speed = (ushort)((mh.id[14] >= '4') ? m_Reader.Read_Intel_ushort() : 8363);
                s.finetune = m_Reader.Read_Intel_short();

                if (m_Reader.isEOF())
                {
                    m_LoadError = "MMERR_LOADING_SAMPLEINFO";
                    return false;
                }

                q.samplename = s.samplename;
                /* The correct formula for the coefficient would be
				   pow(2,(double)s.finetume/OCTAVE/32768), but to avoid floating point
				   here, we'll use a first order approximation here.
				   1/567290 == Ln(2)/OCTAVE/32768 */
                q.speed = (uint)(s.speed + s.speed * (((int)s.speed * (int)s.finetune) / 567290));
                q.length = (uint)(s.sizeend - s.sizestart);
                q.volume = (byte)(s.volume >> 2);
                q.loopstart = (uint)(s.loopstart);
                q.loopend = (uint)(s.loopend);
                q.flags = SharpMikCommon.SF_SIGNED;
                if ((s.flags & ULTS_LOOP) == ULTS_LOOP)
                    q.flags |= SharpMikCommon.SF_LOOP;

                if ((s.flags & ULTS_16BITS) == ULTS_16BITS)
                {
                    s.sizeend += (s.sizeend - s.sizestart);
                    s.sizestart <<= 1;
                    q.flags |= SharpMikCommon.SF_16BITS;
                    q.loopstart >>= 1;
                    q.loopend >>= 1;
                }
            }

            m_Module.positions = new ushort[256];

            for (t = 0; t < 256; t++)
                m_Module.positions[t] = m_Reader.Read_byte();

            for (t = 0; t < 256; t++)
            {
                if (m_Module.positions[t] == 255)
                {
                    m_Module.positions[t] = SharpMikCommon.LAST_PATTERN;
                    break;
                }
            }

            m_Module.numpos = (ushort)t;

            noc = m_Reader.Read_byte();
            nop = m_Reader.Read_byte();

            m_Module.numchn = ++noc;
            m_Module.numpat = ++nop;
            m_Module.numtrk = (ushort)(m_Module.numchn * m_Module.numpat);

            m_Module.AllocTracks();
            m_Module.AllocPatterns();


            for (u = 0; u < m_Module.numchn; u++)
            {
                for (t = 0; t < m_Module.numpat; t++)
                {
                    m_Module.patterns[(t * m_Module.numchn) + u] = (ushort)tracks++;
                }
            }

            /* read pan position table for v1.5 and higher */
            if (mh.id[14] >= '3')
            {
                for (t = 0; t < m_Module.numchn; t++)
                    m_Module.panning[t] = (ushort)(m_Reader.Read_byte() << 4);

                m_Module.flags |= SharpMikCommon.UF_PANNING;
            }

            for (t = 0; t < m_Module.numtrk; t++)
            {
                int rep, row = 0;

                UniReset();
                while (row < 64)
                {
                    rep = ReadUltEvent(ref ev);

                    if (m_Reader.isEOF())
                    {
                        m_LoadError = "MMERR_LOADING_TRACK";
                        return false;
                    }

                    while (rep-- != 0)
                    {
                        byte eff;
                        int m_Modulefset;

                        if (ev.sample != 0)
                            UniInstrument(ev.sample - 1);
                        if (ev.note != 0)
                            UniNote(ev.note + 2 * SharpMikCommon.Octave - 1);

                        /* first effect - various fixes by Alexander Kerkhove and
										  Thomas Neumann */
                        eff = (byte)(ev.eff >> 4);
                        switch (eff)
                        {
                            case 0x3: /* tone portamento */
                                UniEffect(SharpMikCommon.Commands.UNI_ITEFFECTG, ev.dat2);
                                break;
                            case 0x5:
                                break;
                            case 0x9: /* sample m_Modulefset */
                                m_Modulefset = (ev.dat2 << 8) | ((ev.eff & 0xf) == 9 ? ev.dat1 : 0);

                                UniEffect(SharpMikCommon.Commands.UNI_ULTEFFECT9, m_Modulefset);
                                break;
                            case 0xb: /* panning */
                                UniPTEffect(8, ev.dat2 * 0xf);
                                m_Module.flags |= SharpMikCommon.UF_PANNING;
                                break;
                            case 0xc: /* volume */
                                UniPTEffect(eff, ev.dat2 >> 2);
                                break;
                            default:
                                UniPTEffect(eff, ev.dat2);
                                break;
                        }

                        /* second effect */
                        eff = (byte)(ev.eff & 0xf);
                        switch (eff)
                        {
                            case 0x3: /* tone portamento */
                                UniEffect(SharpMikCommon.Commands.UNI_ITEFFECTG, ev.dat1);
                                break;
                            case 0x5:
                                break;
                            case 0x9: /* sample m_Modulefset */
                                if ((ev.eff >> 4) != 9)
                                    UniEffect(SharpMikCommon.Commands.UNI_ULTEFFECT9, ((ushort)ev.dat1) << 8);
                                break;
                            case 0xb: /* panning */
                                UniPTEffect(8, ev.dat1 * 0xf);
                                m_Module.flags |= SharpMikCommon.UF_PANNING;
                                break;
                            case 0xc: /* volume */
                                UniPTEffect(eff, ev.dat1 >> 2);
                                break;
                            default:
                                UniPTEffect(eff, ev.dat1);
                                break;
                        }

                        UniNewline();
                        row++;
                    }
                }

                m_Module.tracks[t] = UniDup();
            }
            return true;
        }

        public override void Cleanup()
        {

        }

        public override string LoadTitle()
        {
            m_Reader.Seek(15, SeekOrigin.Begin);

            string title = m_Reader.Read_String(32);

            return title;
        }
    }
}

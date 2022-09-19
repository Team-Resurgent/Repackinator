using SharpMik.Attributes;
using SharpMik.Extentions;
using SharpMik.Interfaces;

namespace SharpMik.Loaders
{
    [ModFileExtentions(".xm")]
    public class XMLoader : IModLoader
    {
        /*========== Module structure */

        class XMHEADER
        {
            public string id;          /* ID text: 'Extended module: ' */
            public string songname;    /* Module name */
            public string trackername; /* Tracker name */
            public ushort version;         /* Version number */
            public uint headersize;      /* Header size */
            public ushort songlength;      /* Song length (in patten order table) */
            public ushort restart;         /* Restart position */
            public ushort numchn;          /* Number of channels (2,4,6,8,10,...,32) */
            public ushort numpat;          /* Number of patterns (max 256) */
            public ushort numins;          /* Number of instruments (max 128) */
            public ushort flags;
            public ushort tempo;           /* Default tempo */
            public ushort bpm;             /* Default BPM */
            public byte[] orders = new byte[256];     /* Pattern order table  */
        };

        class XMINSTHEADER
        {
            public uint size;     /* Instrument size */
            public string name; /* Instrument name */
            public byte type;     /* Instrument type (always 0) */
            public ushort numsmp;   /* Number of samples in instrument */
            public uint ssize;
        };

        static int XMENVCNT = (12 * 2);
        static int XMNOTECNT = (8 * SharpMikCommon.Octave);

        class XMPATCHHEADER
        {
            public byte[] what = new byte[XMNOTECNT];  /*  Sample number for all notes */
            public ushort[] volenv = new ushort[XMENVCNT]; /*  Points for volume envelope */
            public ushort[] panenv = new ushort[XMENVCNT]; /*  Points for panning envelope */
            public byte volpts;      /*  Number of volume points */
            public byte panpts;      /*  Number of panning points */
            public byte volsus;      /*  Volume sustain point */
            public byte volbeg;      /*  Volume loop start point */
            public byte volend;      /*  Volume loop end point */
            public byte pansus;      /*  Panning sustain point */
            public byte panbeg;      /*  Panning loop start point */
            public byte panend;      /*  Panning loop end point */
            public byte volflg;      /*  Volume type: bit 0: On; 1: Sustain; 2: Loop */
            public byte panflg;      /*  Panning type: bit 0: On; 1: Sustain; 2: Loop */
            public byte vibflg;      /*  Vibrato type */
            public byte vibsweep;    /*  Vibrato sweep */
            public byte vibdepth;    /*  Vibrato depth */
            public byte vibrate;     /*  Vibrato rate */
            public ushort volfade;     /*  Volume fadeout */
        };

        class XMWAVHEADER
        {
            public uint length;         /* Sample length */
            public uint loopstart;      /* Sample loop start */
            public uint looplength;     /* Sample loop length */
            public byte volume;         /* Volume  */
            public sbyte finetune;       /* Finetune (signed byte -128..+127) */
            public byte type;           /* Loop type */
            public byte panning;        /* Panning (0-255) */
            public sbyte relnote;        /* Relative note number (signed byte) */
            public byte reserved;
            public string samplename; /* Sample name */
            public byte vibtype;        /* Vibrato type */
            public byte vibsweep;       /* Vibrato sweep */
            public byte vibdepth;       /* Vibrato depth */
            public byte vibrate;        /* Vibrato rate */
        };

        class XMPATHEADER
        {
            public uint size;     /* Pattern header length  */
            public byte packing;  /* Packing type (always 0) */
            public ushort numrows;  /* Number of rows in pattern (1..256) */
            public short packsize; /* Packed patterndata size */
        };

        class XMNOTE
        {
            public byte note, ins, vol, eff, dat;

            public void Clear()
            {
                note = ins = vol = eff = dat = 0;
            }
        };

        /*========== Loader variables */

        static XMNOTE[]? xmpat = null;
        static XMHEADER? mh = null;

        /* increment unit for sample array reallocation */
        static int XM_SMPINCR = 64;
        static uint[]? nextwav = null;
        static XMWAVHEADER[]? wh = null;
        static int sampHeader = 0;


        public XMLoader()
            : base()
        {
            m_ModuleType = "XM";
            m_ModuleVersion = "XM (FastTracker 2)";
        }

        public override bool Test()
        {
            string? id;
            id = m_Reader?.Read_String(38);

            return id != null && id.StartsWith("Extended Module: ");
        }

        public override bool Init()
        {
            mh = new XMHEADER();

            return true;
        }

        public override void Cleanup()
        {
            mh = null;
        }


        int XM_ReadNote(ref XMNOTE n)
        {
            byte cmp, result = 1;
            n.Clear();
            cmp = m_Reader.Read_byte();

            if ((cmp & 0x80) != 0)
            {
                if ((cmp & 1) != 0) { result++; n.note = m_Reader.Read_byte(); }
                if ((cmp & 2) != 0) { result++; n.ins = m_Reader.Read_byte(); }
                if ((cmp & 4) != 0) { result++; n.vol = m_Reader.Read_byte(); }
                if ((cmp & 8) != 0) { result++; n.eff = m_Reader.Read_byte(); }
                if ((cmp & 16) != 0) { result++; n.dat = m_Reader.Read_byte(); }
            }
            else
            {
                n.note = cmp;
                n.ins = m_Reader.Read_byte();
                n.vol = m_Reader.Read_byte();
                n.eff = m_Reader.Read_byte();
                n.dat = m_Reader.Read_byte();
                result += 4;
            }
            return result;
        }

        byte[] XM_Convert(XMNOTE[] xmtracks, int place, ushort rows)
        {
            int t;
            byte note, ins, vol, eff, dat;

            UniReset();
            for (t = 0; t < rows; t++)
            {
                XMNOTE xmtrack = xmtracks[place++];
                note = xmtrack.note;
                ins = xmtrack.ins;
                vol = xmtrack.vol;
                eff = xmtrack.eff;
                dat = xmtrack.dat;

                if (note != 0)
                {
                    if (note > XMNOTECNT)
                        UniEffect(SharpMikCommon.Commands.UNI_KEYFADE, 0);
                    else
                        UniNote(note - 1);
                }
                if (ins != 0)
                    UniInstrument(ins - 1);

                switch (vol >> 4)
                {
                    case 0x6: /* volslide down */
                        if ((vol & 0xf) != 0) UniEffect(SharpMikCommon.Commands.UNI_XMEFFECTA, vol & 0xf);
                        break;
                    case 0x7: /* volslide up */
                        if ((vol & 0xf) != 0) UniEffect(SharpMikCommon.Commands.UNI_XMEFFECTA, vol << 4);
                        break;

                    /* volume-row fine volume slide is compatible with protracker
                       EBx and EAx effects i.e. a zero nibble means DO NOT SLIDE, as
                       opposed to 'take the last sliding value'. */
                    case 0x8: /* finevol down */
                        UniPTEffect(0xe, 0xb0 | (vol & 0xf));
                        break;
                    case 0x9: /* finevol up */
                        UniPTEffect(0xe, 0xa0 | (vol & 0xf));
                        break;
                    case 0xa: /* set vibrato speed */
                        UniEffect(SharpMikCommon.Commands.UNI_XMEFFECT4, vol << 4);
                        break;
                    case 0xb: /* vibrato */
                        UniEffect(SharpMikCommon.Commands.UNI_XMEFFECT4, vol & 0xf);
                        break;
                    case 0xc: /* set panning */
                        UniPTEffect(0x8, vol << 4);
                        break;
                    case 0xd: /* panning slide left (only slide when data not zero) */
                        if ((vol & 0xf) != 0) UniEffect(SharpMikCommon.Commands.UNI_XMEFFECTP, vol & 0xf);
                        break;
                    case 0xe: /* panning slide right (only slide when data not zero) */
                        if ((vol & 0xf) != 0) UniEffect(SharpMikCommon.Commands.UNI_XMEFFECTP, vol << 4);
                        break;
                    case 0xf: /* tone porta */
                        UniPTEffect(0x3, vol << 4);
                        break;
                    default:
                        if ((vol >= 0x10) && (vol <= 0x50))
                            UniPTEffect(0xc, vol - 0x10);
                        break;
                }

                switch (eff)
                {
                    case 0x4:
                        UniEffect(SharpMikCommon.Commands.UNI_XMEFFECT4, dat);
                        break;
                    case 0x6:
                        UniEffect(SharpMikCommon.Commands.UNI_XMEFFECT6, dat);
                        break;
                    case 0xa:
                        UniEffect(SharpMikCommon.Commands.UNI_XMEFFECTA, dat);
                        break;
                    case 0xe: /* Extended effects */
                        switch (dat >> 4)
                        {
                            case 0x1: /* XM fine porta up */
                                UniEffect(SharpMikCommon.Commands.UNI_XMEFFECTE1, dat & 0xf);
                                break;
                            case 0x2: /* XM fine porta down */
                                UniEffect(SharpMikCommon.Commands.UNI_XMEFFECTE2, dat & 0xf);
                                break;
                            case 0xa: /* XM fine volume up */
                                UniEffect(SharpMikCommon.Commands.UNI_XMEFFECTEA, dat & 0xf);
                                break;
                            case 0xb: /* XM fine volume down */
                                UniEffect(SharpMikCommon.Commands.UNI_XMEFFECTEB, dat & 0xf);
                                break;
                            default:
                                UniPTEffect(eff, dat);
                                break;
                        }
                        break;
                    case 'G' - 55: /* G - set global volume */
                        UniEffect(SharpMikCommon.Commands.UNI_XMEFFECTG, dat > 64 ? 128 : dat << 1);
                        break;
                    case 'H' - 55: /* H - global volume slide */
                        UniEffect(SharpMikCommon.Commands.UNI_XMEFFECTH, dat);
                        break;
                    case 'K' - 55: /* K - keyOff and KeyFade */
                        UniEffect(SharpMikCommon.Commands.UNI_KEYFADE, dat);
                        break;
                    case 'L' - 55: /* L - set envelope position */
                        UniEffect(SharpMikCommon.Commands.UNI_XMEFFECTL, dat);
                        break;
                    case 'P' - 55: /* P - panning slide */
                        UniEffect(SharpMikCommon.Commands.UNI_XMEFFECTP, dat);
                        break;
                    case 'R' - 55: /* R - multi retrig note */
                        UniEffect(SharpMikCommon.Commands.UNI_S3MEFFECTQ, dat);
                        break;
                    case 'T' - 55: /* T - Tremor */
                        UniEffect(SharpMikCommon.Commands.UNI_S3MEFFECTI, dat);
                        break;
                    case 'X' - 55:
                        switch (dat >> 4)
                        {
                            case 1: /* X1 - Extra Fine Porta up */
                                UniEffect(SharpMikCommon.Commands.UNI_XMEFFECTX1, dat & 0xf);
                                break;
                            case 2: /* X2 - Extra Fine Porta down */
                                UniEffect(SharpMikCommon.Commands.UNI_XMEFFECTX2, dat & 0xf);
                                break;
                        }
                        break;
                    default:
                        if (eff <= 0xf)
                        {
                            /* the pattern jump destination is written in decimal,
							   but it seems some poor tracker software writes them
							   in hexadecimal... (sigh) */
                            if (eff == 0xd)
                                /* don't change anything if we're sure it's in hexa */
                                if ((((dat & 0xf0) >> 4) <= 9) && ((dat & 0xf) <= 9))
                                    /* otherwise, convert from dec to hex */
                                    dat = (byte)((((dat & 0xf0) >> 4) * 10) + (dat & 0xf));
                            UniPTEffect(eff, dat);
                        }
                        break;
                }
                UniNewline();
            }
            return UniDup();
        }

        bool LoadPatterns(bool dummypat)
        {
            int t, u, v, numtrk;
            m_Module.AllocTracks();
            m_Module.AllocPatterns();

            numtrk = 0;
            for (t = 0; t < mh.numpat; t++)
            {
                XMPATHEADER ph = new XMPATHEADER();

                ph.size = m_Reader.Read_Intel_uint();
                if (ph.size < (mh.version == 0x0102 ? 8 : 9))
                {
                    m_LoadError = MMERR_LOADING_PATTERN;
                    return false;
                }
                ph.packing = m_Reader.Read_byte();
                if (ph.packing != 0)
                {
                    m_LoadError = MMERR_LOADING_PATTERN;
                    return false;
                }
                if (mh.version == 0x0102)
                    ph.numrows = (ushort)(m_Reader.Read_byte() + 1);
                else
                    ph.numrows = m_Reader.Read_Intel_ushort();
                ph.packsize = (short)m_Reader.Read_Intel_ushort();

                ph.size -= (ushort)(mh.version == 0x0102 ? 8 : 9);
                if (ph.size != 0)
                    m_Reader.Seek((int)ph.size, SeekOrigin.Current);

                m_Module.pattrows[t] = ph.numrows;

                if (ph.numrows != 0)
                {
                    xmpat = new XMNOTE[ph.numrows * m_Module.numchn];
                    for (int i = 0; i < xmpat.Length; i++)
                    {
                        xmpat[i] = new XMNOTE();
                    }

                    /* when packsize is 0, don't try to load a pattern.. it's empty. */
                    if (ph.packsize != 0)
                    {
                        for (u = 0; u < ph.numrows; u++)
                        {
                            for (v = 0; v < m_Module.numchn; v++)
                            {
                                if (ph.packsize == 0)
                                    break;

                                ph.packsize -= (short)XM_ReadNote(ref xmpat[(v * ph.numrows) + u]);
                                if (ph.packsize < 0)
                                {
                                    m_LoadError = MMERR_LOADING_PATTERN;
                                    return false;
                                }
                            }
                        }
                    }

                    if (ph.packsize != 0)
                    {
                        m_Reader.Seek(ph.packsize, SeekOrigin.Current);
                    }

                    if (m_Reader.isEOF())
                    {
                        m_LoadError = MMERR_LOADING_PATTERN;
                        return false;
                    }

                    for (v = 0; v < m_Module.numchn; v++)
                        m_Module.tracks[numtrk++] = XM_Convert(xmpat, v * ph.numrows, ph.numrows);

                    xmpat = null;
                }
                else
                {
                    for (v = 0; v < m_Module.numchn; v++)
                        m_Module.tracks[numtrk++] = XM_Convert(null, 0, ph.numrows);
                }
            }

            if (dummypat)
            {
                m_Module.pattrows[t] = 64;
                xmpat = new XMNOTE[64 * m_Module.numchn];
                for (int i = 0; i < xmpat.Length; i++)
                {
                    xmpat[i] = new XMNOTE();
                }

                for (v = 0; v < m_Module.numchn; v++)
                {
                    m_Module.tracks[numtrk++] = XM_Convert(xmpat, v * 64, 64);
                }
                xmpat = null;
            }

            return true;
        }

        void FixEnvelope(EnvPt[] cur, int pts)
        {
            int u, old, tmp;
            EnvPt prev;
            int place = 0;
            /* Some broken XM editing program will only save the low byte
				of the position value. Try to compensate by adding the
				missing high byte. */

            prev = cur[place++];
            old = prev.pos;

            for (u = 1; u < pts; u++)
            {
                if (cur[place].pos < prev.pos)
                {
                    if (cur[place].pos < 0x100)
                    {
                        if (cur[place].pos > old)   /* same hex century */
                            tmp = cur[place].pos + (prev.pos - old);
                        else
                        {
                            int temp = cur[place].pos;
                            temp = temp | ((prev.pos + 0x100) & 0xff00);
                            tmp = temp;
                        }
                        old = cur[place].pos;
                        cur[place].pos = (short)tmp;
                    }
                    else
                    {
                        old = cur[place].pos;
                    }
                }
                else
                    old = cur[place].pos;

                prev = cur[place++];
            }
        }

        void XM_ProcessEnvelopeVolume(ref INSTRUMENT d, XMPATCHHEADER pth)
        {
            for (int u = 0; u < (XMENVCNT >> 1); u++)
            {
                d.volenv[u].pos = (short)pth.volenv[u << 1];
                d.volenv[u].val = (short)pth.volenv[(u << 1) + 1];
            }
            if ((pth.volflg & 1) != 0) d.volflg |= SharpMikCommon.EF_ON;
            if ((pth.volflg & 2) != 0) d.volflg |= SharpMikCommon.EF_SUSTAIN;
            if ((pth.volflg & 4) != 0) d.volflg |= SharpMikCommon.EF_LOOP;
            d.volsusbeg = d.volsusend = pth.volsus;
            d.volbeg = pth.volbeg;
            d.volend = pth.volend;
            d.volpts = pth.volpts;

            /* scale envelope */
            for (int p = 0; p < XMENVCNT / 2; p++)
                d.volenv[p].val <<= 2;

            if ((d.volflg & SharpMikCommon.EF_ON) != 0 && (d.volpts < 2))
            {
                int flag = d.volflg;
                flag &= ~SharpMikCommon.EF_ON;
                d.volflg = (byte)flag;
            }
        }

        void XM_ProcessEnvelopePan(ref INSTRUMENT d, XMPATCHHEADER pth)
        {
            for (int u = 0; u < (XMENVCNT >> 1); u++)
            {
                d.panenv[u].pos = (short)pth.panenv[u << 1];
                d.panenv[u].val = (short)pth.panenv[(u << 1) + 1];
            }
            if ((pth.panflg & 1) != 0) d.panflg |= SharpMikCommon.EF_ON;
            if ((pth.panflg & 2) != 0) d.panflg |= SharpMikCommon.EF_SUSTAIN;
            if ((pth.panflg & 4) != 0) d.panflg |= SharpMikCommon.EF_LOOP;
            d.pansusbeg = d.pansusend = pth.pansus;
            d.panbeg = pth.panbeg;
            d.panend = pth.panend;
            d.panpts = pth.panpts;

            /* scale envelope */
            for (int p = 0; p < XMENVCNT / 2; p++)
                d.panenv[p].val <<= 2;

            if ((d.panflg & SharpMikCommon.EF_ON) != 0 && (d.panpts < 2))
            {
                int flag = d.panflg;
                flag &= ~SharpMikCommon.EF_ON;
                d.panflg = (byte)flag;
            }
        }

        bool LoadInstruments()
        {
            int t, u, ck;
            INSTRUMENT d;
            uint next = 0;
            ushort wavcnt = 0;

            if (!m_Module.AllocInstruments())
            {
                m_LoadError = MMERR_NOT_A_MODULE;
                return false;
            }

            for (t = 0; t < m_Module.numins; t++)
            {
                d = m_Module.instruments[t];
                XMINSTHEADER ih = new XMINSTHEADER();
                long headend;
                d.samplenumber.Memset(0xff, SharpMikCommon.INSTNOTES);

                /* read instrument header */
                headend = m_Reader.Tell();
                ih.size = m_Reader.Read_Intel_uint();
                headend += ih.size;
                ck = m_Reader.Tell();
                m_Reader.Seek(0, SeekOrigin.End);

                if ((headend < 0) || (m_Reader.Tell() < headend) || (headend < ck))
                {
                    m_Reader.Seek(ck, SeekOrigin.Begin);
                    break;
                }
                m_Reader.Seek(ck, SeekOrigin.Begin);

                ih.name = m_Reader.Read_String(22);
                ih.type = m_Reader.Read_byte();
                ih.numsmp = m_Reader.Read_Intel_ushort();

                d.insname = ih.name;

                if ((short)ih.size > 29)
                {
                    ih.ssize = m_Reader.Read_Intel_uint();
                    if (((short)ih.numsmp > 0) && (ih.numsmp <= XMNOTECNT))
                    {
                        XMPATCHHEADER pth = new XMPATCHHEADER();
                        m_Reader.Read_bytes(pth.what, XMNOTECNT);
                        m_Reader.Read_Intel_ushorts(pth.volenv, XMENVCNT);
                        m_Reader.Read_Intel_ushorts(pth.panenv, XMENVCNT);
                        pth.volpts = m_Reader.Read_byte();
                        pth.panpts = m_Reader.Read_byte();
                        pth.volsus = m_Reader.Read_byte();
                        pth.volbeg = m_Reader.Read_byte();
                        pth.volend = m_Reader.Read_byte();
                        pth.pansus = m_Reader.Read_byte();
                        pth.panbeg = m_Reader.Read_byte();
                        pth.panend = m_Reader.Read_byte();
                        pth.volflg = m_Reader.Read_byte();
                        pth.panflg = m_Reader.Read_byte();
                        pth.vibflg = m_Reader.Read_byte();
                        pth.vibsweep = m_Reader.Read_byte();
                        pth.vibdepth = m_Reader.Read_byte();
                        pth.vibrate = m_Reader.Read_byte();
                        pth.volfade = m_Reader.Read_Intel_ushort();

                        /* read the remainder of the header
						   (2 bytes for 1.03, 22 for 1.04) */
                        if (headend >= m_Reader.Tell())
                            for (u = (int)(headend - m_Reader.Tell()); u != 0; u--)
                                m_Reader.Read_byte();

                        /* we can't trust the envelope point count here, as some
						   modules have incorrect values (K_OSPACE.XM reports 32 volume
						   points, for example). */
                        if (pth.volpts > XMENVCNT / 2)
                            pth.volpts = (byte)(XMENVCNT / 2);
                        if (pth.panpts > XMENVCNT / 2)
                            pth.panpts = (byte)(XMENVCNT / 2);

                        if ((m_Reader.isEOF()) || (pth.volpts > XMENVCNT / 2) || (pth.panpts > XMENVCNT / 2))
                        {
                            if (nextwav != null)
                            {
                                nextwav = null;
                            }
                            if (wh != null)
                            {
                                wh = null;
                            }
                            m_LoadError = MMERR_LOADING_SAMPLEINFO;
                            return false;
                        }

                        for (u = 0; u < XMNOTECNT; u++)
                            d.samplenumber[u] = (ushort)(pth.what[u] + m_Module.numsmp);
                        d.volfade = pth.volfade;


                        XM_ProcessEnvelopeVolume(ref d, pth);
                        XM_ProcessEnvelopePan(ref d, pth);


                        if ((d.volflg & SharpMikCommon.EF_ON) != 0)
                            FixEnvelope(d.volenv, d.volpts);
                        if ((d.panflg & SharpMikCommon.EF_ON) != 0)
                            FixEnvelope(d.panenv, d.panpts);

                        /* Samples are stored outside the instrument struct now, so we
						   have to load them all into a temp area, count the m_Module.numsmp
						   along the way and then do an AllocSamples() and move
						   everything over */
                        if (mh.version > 0x0103)
                            next = 0;

                        for (u = 0; u < ih.numsmp; u++)
                        {
                            XMWAVHEADER s = null;
                            if (wh != null && sampHeader < wh.Length)
                            {
                                s = wh[sampHeader];
                            }

                            /* Allocate more room for sample information if necessary */
                            if (m_Module.numsmp + u == wavcnt)
                            {
                                ushort lastSize = wavcnt;
                                wavcnt += (ushort)XM_SMPINCR;

                                if (nextwav == null)
                                {
                                    nextwav = new uint[wavcnt];
                                }
                                else
                                {
                                    Array.Resize(ref nextwav, wavcnt);
                                }

                                if (wh == null)
                                {
                                    wh = new XMWAVHEADER[wavcnt];
                                }
                                else
                                {
                                    Array.Resize(ref wh, wavcnt);
                                }

                                for (ushort i = lastSize; i < wavcnt; i++)
                                {
                                    wh[i] = new XMWAVHEADER();
                                }


                                //sampHeader=(wavcnt-XM_SMPINCR);
                                s = wh[sampHeader];
                            }

                            s.length = m_Reader.Read_Intel_uint();
                            s.loopstart = m_Reader.Read_Intel_uint();
                            s.looplength = m_Reader.Read_Intel_uint();
                            s.volume = m_Reader.Read_byte();
                            s.finetune = m_Reader.Read_sbyte();
                            s.type = m_Reader.Read_byte();
                            s.panning = m_Reader.Read_byte();
                            s.relnote = m_Reader.Read_sbyte();
                            s.vibtype = pth.vibflg;
                            s.vibsweep = pth.vibsweep;
                            s.vibdepth = (byte)(pth.vibdepth * 4);
                            s.vibrate = pth.vibrate;
                            s.reserved = m_Reader.Read_byte();
                            s.samplename = m_Reader.Read_String(22);

                            nextwav[m_Module.numsmp + u] = next;
                            next += s.length;

                            if (m_Reader.isEOF())
                            {
                                nextwav = null;
                                wh = null;
                                m_LoadError = MMERR_LOADING_SAMPLEINFO;
                                return false;
                            }

                            sampHeader++;
                        }

                        if (mh.version > 0x0103)
                        {
                            for (u = 0; u < ih.numsmp; u++)
                            {
                                nextwav[m_Module.numsmp++] += (uint)m_Reader.Tell();
                            }
                            m_Reader.Seek((int)next, SeekOrigin.Current);
                        }
                        else
                            m_Module.numsmp += ih.numsmp;
                    }
                    else
                    {
                        /* read the remainder of the header */
                        ck = m_Reader.Tell();
                        m_Reader.Seek(0, SeekOrigin.End);

                        if ((headend < 0) || (m_Reader.Tell() < headend) || (headend < ck))
                        {
                            m_Reader.Seek(ck, SeekOrigin.Begin);
                            break;
                        }
                        m_Reader.Seek(ck, SeekOrigin.Begin);

                        u = (int)(headend - m_Reader.Tell());
                        for (; u != 0; u--)
                            m_Reader.Read_byte();

                        if (m_Reader.isEOF())
                        {
                            nextwav = null;
                            wh = null;
                            m_LoadError = MMERR_LOADING_SAMPLEINFO;
                            return false;
                        }
                    }
                }
            }

            /* sanity check */
            if (m_Module.numsmp == 0)
            {
                nextwav = null;
                wh = null;
                m_LoadError = MMERR_LOADING_SAMPLEINFO;
                return false;
            }

            return true;
        }

        public override bool Load(int curious)
        {
            INSTRUMENT d;
            SAMPLE q;
            int t, u;
            bool dummypat = false;
            char[] tracker = new char[21];
            //char[] modtype = new char[60];

            /* try to read module header */
            mh.id = m_Reader.Read_String(17);
            mh.songname = m_Reader.Read_String(21);
            mh.trackername = m_Reader.Read_String(20);

            mh.version = m_Reader.Read_Intel_ushort();
            if ((mh.version < 0x102) || (mh.version > 0x104))
            {
                m_LoadError = MMERR_NOT_A_MODULE;
                return false;
            }
            mh.headersize = m_Reader.Read_Intel_uint();
            mh.songlength = m_Reader.Read_Intel_ushort();
            mh.restart = m_Reader.Read_Intel_ushort();
            mh.numchn = m_Reader.Read_Intel_ushort();
            mh.numpat = m_Reader.Read_Intel_ushort();
            mh.numins = m_Reader.Read_Intel_ushort();
            mh.flags = m_Reader.Read_Intel_ushort();
            mh.tempo = m_Reader.Read_Intel_ushort();
            mh.bpm = m_Reader.Read_Intel_ushort();

            if (mh.bpm == 0 || mh.songlength > 256)
            {
                m_LoadError = MMERR_NOT_A_MODULE;
                return false;
            }

            m_Reader.Read_bytes(mh.orders, mh.songlength);
            if (!m_Reader.Seek((int)(mh.headersize + 60), SeekOrigin.Begin) || m_Reader.isEOF())
            {
                m_LoadError = MMERR_LOADING_HEADER;
                return false;
            }

            /* set module variables */
            m_Module.initspeed = (byte)mh.tempo;
            m_Module.inittempo = mh.bpm;

            tracker = mh.trackername.ToCharArray();
            for (t = tracker.Length - 1; (t >= 0) && (tracker[t] <= ' '); t--)
                tracker[t] = (char)0;

            /* some modules have the tracker name empty */
            if (tracker[0] == 0)
            {
                tracker = "Unknown tracker".ToCharArray();
            }

            string modType = string.Format("{0} (XM format {1}.{2})", tracker, mh.version >> 8, mh.version & 0xff);

            m_Module.modtype = modType;
            m_Module.numchn = (byte)mh.numchn;
            m_Module.numpat = mh.numpat;
            m_Module.numtrk = (ushort)(m_Module.numpat * m_Module.numchn);   /* get number of channels */
            m_Module.songname = mh.songname;
            m_Module.numpos = mh.songlength;               /* copy the songlength */
            m_Module.reppos = (ushort)(mh.restart < mh.songlength ? mh.restart : 0);
            m_Module.numins = mh.numins;
            m_Module.flags |= SharpMikCommon.UF_XMPERIODS | SharpMikCommon.UF_INST | SharpMikCommon.UF_NOWRAP | SharpMikCommon.UF_FT2QUIRKS | SharpMikCommon.UF_PANNING;
            if ((mh.flags & 1) != 0)
                m_Module.flags |= SharpMikCommon.UF_LINEAR;

            m_Module.bpmlimit = 32;

            m_Module.chanvol.Memset(64, m_Module.numchn);           /* store channel volumes */

            m_Module.positions = new ushort[m_Module.numpos + 1];
            for (t = 0; t < m_Module.numpos; t++)
                m_Module.positions[t] = mh.orders[t];

            /* We have to check for any pattern numbers in the order list greater than
			   the number of patterns total. If one or more is found, we set it equal to
			   the pattern total and make a dummy pattern to workaround the problem */
            for (t = 0; t < m_Module.numpos; t++)
            {
                if (m_Module.positions[t] >= m_Module.numpat)
                {
                    m_Module.positions[t] = m_Module.numpat;
                    dummypat = true;
                }
            }
            if (dummypat)
            {
                m_Module.numpat++; m_Module.numtrk += m_Module.numchn;
            }

            if (mh.version < 0x0104)
            {
                if (!LoadInstruments()) return false;
                if (!LoadPatterns(dummypat)) return false;
                for (t = 0; t < m_Module.numsmp; t++)
                    nextwav[t] += (uint)m_Reader.Tell();
            }
            else
            {
                if (!LoadPatterns(dummypat)) return false;
                if (!LoadInstruments()) return false;
            }

            m_Module.AllocSamples();

            sampHeader = 0;
            for (u = 0; u < m_Module.numsmp; u++)
            {
                q = m_Module.samples[u];
                XMWAVHEADER s = wh[sampHeader++];

                q.samplename = s.samplename;
                q.length = s.length;
                q.loopstart = s.loopstart;
                q.loopend = (uint)(s.loopstart + s.looplength);
                q.volume = s.volume;
                q.speed = (uint)s.finetune + 128;
                q.panning = s.panning;
                q.seekpos = nextwav[u];
                q.vibtype = s.vibtype;
                q.vibsweep = s.vibsweep;
                q.vibdepth = s.vibdepth;
                q.vibrate = s.vibrate;

                if ((s.type & 0x10) != 0)
                {
                    q.length >>= 1;
                    q.loopstart >>= 1;
                    q.loopend >>= 1;
                }

                q.flags |= SharpMikCommon.SF_OWNPAN | SharpMikCommon.SF_DELTA | SharpMikCommon.SF_SIGNED;
                if ((s.type & 0x3) != 0) q.flags |= SharpMikCommon.SF_LOOP;
                if ((s.type & 0x2) != 0) q.flags |= SharpMikCommon.SF_BIDI;
                if ((s.type & 0x10) != 0) q.flags |= SharpMikCommon.SF_16BITS;
            }


            sampHeader = 0;
            for (u = 0; u < m_Module.numins; u++)
            {
                d = m_Module.instruments[u];
                for (t = 0; t < XMNOTECNT; t++)
                {
                    if (d.samplenumber[t] >= m_Module.numsmp)
                        d.samplenote[t] = 255;
                    else
                    {
                        int note = t + wh[d.samplenumber[t]].relnote;
                        d.samplenote[t] = (byte)((note < 0) ? 0 : note);
                    }
                }
            }

            wh = null;
            nextwav = null;
            return true;
        }



        public override string LoadTitle()
        {
            throw new NotImplementedException();
        }
    }
}

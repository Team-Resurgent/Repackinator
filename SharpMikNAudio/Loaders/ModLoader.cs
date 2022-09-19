using SharpMik.Attributes;
using SharpMik.Interfaces;

namespace SharpMik.Loaders
{
    [ModFileExtentions(".mod")]
    public class ModLoader : IModLoader
    {
        #region sub Classes
        class ModuleSampleInfo
        {
            public string samplename;       /* 22 in module, 23 in memory */
            public ushort length;
            public byte finetune;
            public byte volume;
            public ushort reppos;
            public ushort replen;
        }

        class ModuleHeader
        {
            public ModuleHeader()
            {
                samples = new ModuleSampleInfo[31];

                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] = new ModuleSampleInfo();
                }

                positions = new byte[128];
                magic2 = new byte[4];
            }

            public string songname;         /* the songname.. 20 in module, 21 in memory */
            public ModuleSampleInfo[] samples;      /* all sampleinfo */
            public byte songlength;         /* number of patterns used */
            public byte magic1;             /* should be 127 */
            public byte[] positions;        /* which pattern to play at pos */
            public byte[] magic2;           /* string "M.K." or "FLT4" or "FLT8" */
        }

        class ModuleNote
        {
            public byte a, b, c, d;
        }
        #endregion


        #region const variables

        const string protracker = "Protracker";
        const string startrekker = "Startrekker";
        const string fasttracker = "Fasttracker";
        const string oktalyser = "Oktalyser";
        const string oktalyzer = "Oktalyzer";
        const string taketracker = "TakeTracker";
        const string orpheus = "Imago Orpheus (MOD format)";
        const int MODULEHEADERSIZE = 1084;

        #endregion



        #region private variables

        ModuleHeader? mh = null;
        ModuleNote[]? patbuf = null;
        int modtype, trekker;
        #endregion


        public ModLoader()
        {
            m_ModuleType = "Standard module";
            m_ModuleVersion = "MOD (31 instruments)";
        }


        #region interface impementation
        public override bool Init()
        {
            mh = new ModuleHeader();

            return true;
        }

        public override bool Test()
        {
            byte[] id = new byte[4];
            byte numberOfChannels = 0;
            string description = "";

            m_Reader.Seek(MODULEHEADERSIZE - 4, SeekOrigin.Begin);

            if (m_Reader.Read(id, 0, 4) != 4)
                return false;

            return MOD_CheckType(id, ref numberOfChannels, ref description);
        }

        public override void Cleanup()
        {
            mh = null;
            patbuf = null;
            m_Module = null;
        }

        public override string LoadTitle()
        {
            m_Reader.Seek(0, System.IO.SeekOrigin.Begin);
            string title = m_Reader.Read_String(20);

            return title;
        }

        public override bool Load(int curious)
        {
            return LoadModule(curious);
        }
        #endregion


        #region Class Implementation
        bool MOD_CheckType(byte[] id, ref byte numchn, ref string descr)
        {
            char[] idChar = new char[id.Length];

            for (int i = 0; i < id.Length; i++)
            {
                idChar[i] = (char)id[i];
            }

            string idString = new string(idChar);

            modtype = trekker = 0;

            /* Protracker and variants */
            if (idString == "M.K." || idString == "M!K!")
            {
                descr = protracker;
                modtype = 0;
                numchn = 4;
                return true;
            }

            /* Star Tracker */
            if ((idString.StartsWith("FLT") || idString.StartsWith("EXO")) && char.IsDigit(idChar[3]))
            {
                descr = startrekker;
                modtype = trekker = 1;
                numchn = (byte)(id[3] - '0');
                if (numchn == 4 || numchn == 8)
                    return true;
                else
                    Console.WriteLine("\rUnknown FLT{0} module type\n", numchn);
                return false;
            }

            /* Oktalyzer (Amiga) */
            if (idString == "OKTA")
            {
                descr = oktalyzer;
                modtype = 1;
                numchn = 8;
                return true;
            }

            /* Oktalyser (Atari) */
            if (idString == "CD81")
            {
                descr = oktalyser;
                modtype = 1;
                numchn = 8;
                return true;
            }

            /* Fasttracker */
            if (idString.EndsWith("CHN") && char.IsDigit(idChar[0]))
            {
                descr = fasttracker;
                modtype = 1;
                numchn = (byte)(id[0] - '0');
                return true;
            }
            /* Fasttracker or Taketracker */
            if ((idString.EndsWith("CH") || idString.EndsWith("CN")) && (char.IsDigit(idChar[0]) && char.IsDigit(idChar[1])))
            {
                if (id[3] == 'H')
                {
                    descr = fasttracker;
                    modtype = 2;        /* this can also be Imago Orpheus */
                }
                else
                {
                    descr = taketracker;
                    modtype = 1;
                }
                numchn = (byte)((id[0] - '0') * 10 + (id[1] - '0'));
                return true;
            }

            return false;
        }




        bool LoadModule(int curious)
        {
            int t, scan;
            SAMPLE q;
            ModuleSampleInfo s;
            string descr = "";
            mh.songname = m_Reader.Read_String(20);

            // Load the Sample info
            for (t = 0; t < 31; t++)
            {
                s = mh.samples[t];
                s.samplename = m_Reader.Read_String(22);
                s.length = m_Reader.Read_Motorola_ushort();
                s.finetune = m_Reader.Read_byte();
                s.volume = m_Reader.Read_byte();
                s.reppos = m_Reader.Read_Motorola_ushort();
                s.replen = m_Reader.Read_Motorola_ushort();
            }

            mh.songlength = m_Reader.Read_byte();

            /* this fixes mods which declare more than 128 positions. 
			 * eg: beatwave.mod */
            if (mh.songlength > 128)
            {
                mh.songlength = 128;
            }


            mh.magic1 = m_Reader.Read_byte();
            m_Reader.Read_bytes(mh.positions, 128);
            m_Reader.Read_bytes(mh.magic2, 4);

            if (m_Reader.isEOF())
            {
                //_mm_errno = MMERR_LOADING_HEADER;
                return false;
            }

            m_Module.initspeed = 6;
            m_Module.inittempo = 125;


            if (!(MOD_CheckType(mh.magic2, ref m_Module.numchn, ref descr)))
            {
                // _mm_errno = MMERR_NOT_A_MODULE;
                return false;
            }


            if (trekker != 0 && m_Module.numchn == 8)
            {
                for (t = 0; t < 128; t++)
                {
                    /* if module pretends to be FLT8, yet the order table
					   contains odd numbers, chances are it's a lying FLT4... */
                    if ((mh.positions[t] & 1) != 0)
                    {
                        m_Module.numchn = 4;
                        break;
                    }
                }
            }

            if (trekker != 0 && m_Module.numchn == 8)
            {
                for (t = 0; t < 128; t++)
                {
                    mh.positions[t] >>= 1;
                }
            }

            m_Module.songname = mh.songname;
            m_Module.numpos = mh.songlength;
            m_Module.reppos = 0;

            /* Count the number of patterns */
            m_Module.numpat = 0;
            for (t = 0; t < m_Module.numpos; t++)
            {
                if (mh.positions[t] > m_Module.numpat)
                {
                    m_Module.numpat = mh.positions[t];
                }
            }

            /* since some old modules embed extra patterns, we have to check the
			 whole list to get the samples' file offsets right - however we can find
			 garbage here, so check carefully */
            scan = 1;
            for (t = m_Module.numpos; t < 128; t++)
            {
                if (mh.positions[t] >= 0x80)
                {
                    scan = 0;
                }
            }

            if (scan != 0)
            {
                for (t = m_Module.numpos; t < 128; t++)
                {
                    if (mh.positions[t] > m_Module.numpat)
                    {
                        m_Module.numpat = mh.positions[t];
                    }
                    if ((curious != 0) && (mh.positions[t] != 0))
                    {
                        m_Module.numpos = (ushort)(t + 1);
                    }
                }
            }

            m_Module.numpat++;
            m_Module.numtrk = (ushort)(m_Module.numpat * m_Module.numchn);

            m_Module.positions = new ushort[m_Module.numpos];

            for (t = 0; t < m_Module.numpos; t++)
            {
                m_Module.positions[t] = mh.positions[t];
            }

            /* Finally, init the sampleinfo structures  */
            m_Module.numins = m_Module.numsmp = 31;
            m_Module.AllocSamples();

            for (t = 0; t < m_Module.numins; t++)
            {
                s = mh.samples[t];
                q = m_Module.samples[t];

                /* convert the samplename */
                q.samplename = s.samplename;
                /* init the sampleinfo variables and convert the size pointers */
                q.speed = SharpMikCommon.finetune[s.finetune & 0xf];
                q.volume = (byte)(s.volume & 0x7f);
                q.loopstart = (uint)s.reppos << 1;
                q.loopend = q.loopstart + ((uint)s.replen << 1);
                q.length = (uint)s.length << 1;
                q.flags = SharpMikCommon.SF_SIGNED;
                /* Imago Orpheus creates MODs with 16 bit samples, check */
                if ((modtype == 2) && ((s.volume & 0x80) != 0))
                {
                    q.flags |= SharpMikCommon.SF_16BITS;
                    descr = orpheus;
                }
                if (s.replen > 2)
                {
                    q.flags |= SharpMikCommon.SF_LOOP;
                }
            }

            m_Module.modtype = descr;

            return ML_LoadPatterns();
        }



        bool ML_LoadPatterns()
        {
            int t, s, tracks = 0;
            m_Module.AllocPatterns();
            m_Module.AllocTracks();

            /* Allocate temporary buffer for loading and converting the patterns */
            patbuf = new ModuleNote[64 * m_Module.numchn];

            for (int i = 0; i < patbuf.Length; i++)
            {
                patbuf[i] = new ModuleNote();
            }


            if (trekker != 0 && m_Module.numchn == 8)
            {
                /* Startrekker module dual pattern */
                for (t = 0; t < m_Module.numpat; t++)
                {
                    for (s = 0; s < (64U * 4); s++)
                    {
                        patbuf[s].a = m_Reader.Read_byte();
                        patbuf[s].b = m_Reader.Read_byte();
                        patbuf[s].c = m_Reader.Read_byte();
                        patbuf[s].d = m_Reader.Read_byte();
                    }
                    for (s = 0; s < 4; s++)
                    {
                        if (!ConvertTrack(patbuf, s, 4, ref m_Module.tracks, tracks++))
                        {
                            return false;
                        }
                    }

                    for (s = 0; s < (64U * 4); s++)
                    {
                        patbuf[s].a = m_Reader.Read_byte();
                        patbuf[s].b = m_Reader.Read_byte();
                        patbuf[s].c = m_Reader.Read_byte();
                        patbuf[s].d = m_Reader.Read_byte();
                    }

                    for (s = 0; s < 4; s++)
                    {
                        if (!ConvertTrack(patbuf, s, 4, ref m_Module.tracks, tracks++))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                /* Generic module pattern */
                for (t = 0; t < m_Module.numpat; t++)
                {
                    /* Load the pattern into the temp buffer and convert it */
                    for (s = 0; s < (64U * m_Module.numchn); s++)
                    {
                        patbuf[s].a = m_Reader.Read_byte();
                        patbuf[s].b = m_Reader.Read_byte();
                        patbuf[s].c = m_Reader.Read_byte();
                        patbuf[s].d = m_Reader.Read_byte();
                    }

                    for (s = 0; s < m_Module.numchn; s++)
                    {
                        if (!ConvertTrack(patbuf, s, m_Module.numchn, ref m_Module.tracks, tracks++))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }


        bool ConvertTrack(ModuleNote[] n, int startlocation, int numchn, ref byte[][] tracks, int track)
        {
            int t;
            byte lasteffect = 0x10; /* non existant effect */

            UniReset();
            int place = startlocation;
            for (t = 0; t < 64; t++)
            {
                lasteffect = ConvertNote(n, place, lasteffect);
                UniNewline();
                place += numchn;
            }

            tracks[track] = UniDup();
            return true;
        }

        byte ConvertNote(ModuleNote[] n, int place, byte lasteffect)
        {
            byte instrument, effect, effdat, note;
            ushort period;
            byte lastnote = 0;

            /* extract the various information from the 4 bytes that make up a note */
            instrument = (byte)((n[place].a & 0x10) | (n[place].c >> 4));
            period = (ushort)((((ushort)n[place].a & 0xf) << 8) + n[place].b);
            effect = (byte)(n[place].c & 0xf);
            effdat = n[place].d;

            /* Convert the period to a note number */
            note = 0;
            if (period != 0)
            {
                for (note = 0; note < SharpMikCommon.Npertab.Length; note++)
                {
                    if (period >= SharpMikCommon.Npertab[note])
                    {
                        break;
                    }
                }
                if (note == SharpMikCommon.Npertab.Length)
                {
                    note = 0;
                }
                else
                {
                    note++;
                }
            }

            if (instrument != 0)
            {
                /* if instrument does not exist, note cut */
                if ((instrument > 31) || (mh.samples[instrument - 1].length == 0))
                {
                    UniPTEffect(0xc, 0);
                    if (effect == 0xc)
                        effect = effdat = 0;
                }
                else
                {
                    /* Protracker handling */
                    if (modtype == 0)
                    {
                        /* if we had a note, then change instrument... */
                        if (note != 0)
                        {
                            UniInstrument(instrument - 1);
                        }
                        /* ...otherwise, only adjust volume... */
                        else
                        {
                            /* ...unless an effect was specified, which forces a new
							   note to be played */
                            if (effect != 0 || effdat != 0)
                            {
                                UniInstrument(instrument - 1);
                                note = lastnote;
                            }
                            else
                                UniPTEffect(0xc, (byte)(mh.samples[instrument - 1].volume & 0x7f));
                        }
                    }
                    else
                    {
                        /* Fasttracker handling */
                        UniInstrument(instrument - 1);
                        if (note == 0)
                        {
                            note = lastnote;
                        }
                    }
                }
            }
            if (note != 0)
            {
                UniNote(note + 2 * SharpMikCommon.Octave - 1);
                lastnote = note;
            }

            /* Convert pattern jump from Dec to Hex */
            if (effect == 0xd)
            {
                effdat = (byte)((((effdat & 0xf0) >> 4) * 10) + (effdat & 0xf));
            }

            /* Volume slide, up has priority */
            if ((effect == 0xa) && (effdat & 0xf) != 0 && (effdat & 0xf0) != 0)
            {
                effdat &= 0xf0;
            }

            /* Handle ``heavy'' volumes correctly */
            if ((effect == 0xc) && (effdat > 0x40))
            {
                effdat = 0x40;
            }

            /* An isolated 100, 200 or 300 effect should be ignored (no
			   "standalone" porta memory in mod files). However, a sequence such
			   as 1XX, 100, 100, 100 is fine. */
            if ((effdat == 0) && ((effect == 1) || (effect == 2) || (effect == 3)) && (lasteffect < 0x10) && (effect != lasteffect))
            {
                effect = 0;
            }

            UniPTEffect(effect, effdat);
            if (effect == 8)
            {
                m_Module.flags |= SharpMikCommon.UF_PANNING;
            }

            return effect;
        }

        #endregion
    }
}

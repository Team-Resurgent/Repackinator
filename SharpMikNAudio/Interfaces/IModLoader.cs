using SharpMik.Extentions;
using SharpMik.IO;
using SharpMik.Player;

namespace SharpMik.Interfaces
{
    public abstract class IModLoader
    {
        private munitrk? m_Tracker;

        #region protected variables		
        protected string? m_ModuleType;
        protected string? m_ModuleVersion;

        protected ModuleReader? m_Reader;

        protected MikModule? m_Module;

        protected string? m_LoadError;

        public virtual string[]? FileExtentions { get; }
        #endregion


        #region Common Loader variables

        protected sbyte[] remap = new sbyte[SharpMikCommon.UF_MAXCHAN];/* for removing empty channels */
        protected byte[]? poslookup = null;/* lookup table for pattern jumps after blank  pattern removal */

        protected ushort poslookupcnt;
        protected ushort[] origpositions;

        protected bool filters;             /* resonant filters in use */
        protected byte activemacro;         /* active midi macro number for Sxx,xx<80h */
        protected byte[] filtermacros = new byte[SharpMikCommon.UF_MAXMACRO]; /* midi macro settings */
        protected FILTER[] filtersettings;  /* computed filter settings */


        protected int[]? noteindex = null;      /* remap value for linear period modules */
        protected int noteindexcount = 0;
        #endregion


        #region Loader Errors
        public const string MMERR_LOADING_PATTERN = "MMERR_LOADING_PATTERN";
        public const string MMERR_LOADING_HEADER = "MMERR_LOADING_HEADER";
        public const string MMERR_LOADING_SAMPLEINFO = "MMERR_LOADING_SAMPLEINFO";
        public const string MMERR_NOT_A_MODULE = "MMERR_NOT_A_MODULE";
        public const string MMERR_LOADING_TRACK = "MMERR_LOADING_TRACK";
        public const string MMERR_MED_SYNTHSAMPLES = "MMERR_MED_SYNTHSAMPLES";
        #endregion


        #region public accessors
        public string? ModuleType
        {
            get { return m_ModuleType; }
        }

        public string? ModuleVersion
        {
            get { return m_ModuleVersion; }
        }

        public ModuleReader ModuleReader
        {
            set { m_Reader = value; }
        }

        public MikModule Module
        {
            get { return m_Module; }
            set { m_Module = value; }
        }

        public munitrk Tracker
        {
            get { return m_Tracker; }
            set { m_Tracker = value; }
        }

        public string LoadError
        {
            get { return m_LoadError; }
        }
        #endregion



        public IModLoader()
        {
            filtersettings = new FILTER[SharpMikCommon.UF_MAXFILTER];

            for (int i = 0; i < filtersettings.Length; i++)
            {
                filtersettings[i] = new FILTER();
            }
        }


        #region Common Loader Functions

        protected void S3MIT_CreateOrders(int curious)
        {
            int t;

            m_Module.numpos = 0;
            m_Module.positions.Memset(0, poslookupcnt);
            poslookup.Memset(byte.MaxValue, 256);

            for (t = 0; t < poslookupcnt; t++)
            {
                int order = origpositions[t];
                if (order == 255) order = SharpMikCommon.LAST_PATTERN;
                m_Module.positions[m_Module.numpos] = (ushort)order;
                poslookup[t] = (byte)m_Module.numpos; /* bug fix for freaky S3Ms / ITs */

                if (origpositions[t] < 254)
                    m_Module.numpos++;
                else
                    /* end of song special order */
                    if ((order == SharpMikCommon.LAST_PATTERN) && (curious--) == 0) break;
            }
        }



        protected void S3MIT_ProcessCmd(byte cmd, byte inf, uint flags)
        {
            byte lo = (byte)(inf & 0xf);


            /* process S3M / IT specific command structure */

            if (cmd != 255)
            {
                switch (cmd)
                {
                    case 1: /* Axx set speed to xx */
                        UniEffect(SharpMikCommon.Commands.UNI_S3MEFFECTA, inf);
                        break;
                    case 2: /* Bxx position jump */
                        if (inf < poslookupcnt)
                        {
                            /* switch to curious mode if necessary, for example
							   sympex.it, deep joy.it */
                            if (((sbyte)poslookup[inf] < 0) && (origpositions[inf] != 255))
                                S3MIT_CreateOrders(1);

                            if (!((sbyte)poslookup[inf] < 0))
                                UniPTEffect(0xb, poslookup[inf]);
                        }
                        break;
                    case 3: /* Cxx patternbreak to row xx */
                        if ((flags & SharpMikCommon.S3MIT_OLDSTYLE) == SharpMikCommon.S3MIT_OLDSTYLE && !((flags & SharpMikCommon.S3MIT_IT) == SharpMikCommon.S3MIT_IT))
                            UniPTEffect(0xd, (inf >> 4) * 10 + (inf & 0xf));
                        else
                            UniPTEffect(0xd, inf);
                        break;
                    case 4: /* Dxy volumeslide */
                        UniEffect(SharpMikCommon.Commands.UNI_S3MEFFECTD, inf);
                        break;
                    case 5: /* Exy toneslide down */
                        UniEffect(SharpMikCommon.Commands.UNI_S3MEFFECTE, inf);
                        break;
                    case 6: /* Fxy toneslide up */
                        UniEffect(SharpMikCommon.Commands.UNI_S3MEFFECTF, inf);
                        break;
                    case 7: /* Gxx Tone portamento, speed xx */
                        if ((flags & SharpMikCommon.S3MIT_OLDSTYLE) == SharpMikCommon.S3MIT_OLDSTYLE)
                            UniPTEffect(0x3, inf);
                        else
                            UniEffect(SharpMikCommon.Commands.UNI_ITEFFECTG, inf);
                        break;
                    case 8: /* Hxy vibrato */
                        if ((flags & SharpMikCommon.S3MIT_OLDSTYLE) == SharpMikCommon.S3MIT_OLDSTYLE)
                            UniPTEffect(0x4, inf);
                        else
                            UniEffect(SharpMikCommon.Commands.UNI_ITEFFECTH, inf);
                        break;
                    case 9: /* Ixy tremor, ontime x, offtime y */
                        if ((flags & SharpMikCommon.S3MIT_OLDSTYLE) == SharpMikCommon.S3MIT_OLDSTYLE)
                            UniEffect(SharpMikCommon.Commands.UNI_S3MEFFECTI, inf);
                        else
                            UniEffect(SharpMikCommon.Commands.UNI_ITEFFECTI, inf);
                        break;
                    case 0xa: /* Jxy arpeggio */
                        UniPTEffect(0x0, inf);
                        break;
                    case 0xb: /* Kxy Dual command H00 & Dxy */
                        if ((flags & SharpMikCommon.S3MIT_OLDSTYLE) == SharpMikCommon.S3MIT_OLDSTYLE)
                            UniPTEffect(0x4, 0);
                        else
                            UniEffect(SharpMikCommon.Commands.UNI_ITEFFECTH, 0);
                        UniEffect(SharpMikCommon.Commands.UNI_S3MEFFECTD, inf);
                        break;
                    case 0xc: /* Lxy Dual command G00 & Dxy */
                        if ((flags & SharpMikCommon.S3MIT_OLDSTYLE) == SharpMikCommon.S3MIT_OLDSTYLE)
                            UniPTEffect(0x3, 0);
                        else
                            UniEffect(SharpMikCommon.Commands.UNI_ITEFFECTG, 0);
                        UniEffect(SharpMikCommon.Commands.UNI_S3MEFFECTD, inf);
                        break;
                    case 0xd: /* Mxx Set Channel Volume */
                        UniEffect(SharpMikCommon.Commands.UNI_ITEFFECTM, inf);
                        break;
                    case 0xe: /* Nxy Slide Channel Volume */
                        UniEffect(SharpMikCommon.Commands.UNI_ITEFFECTN, inf);
                        break;
                    case 0xf: /* Oxx set sampleoffset xx00h */
                        UniPTEffect(0x9, inf);
                        break;
                    case 0x10: /* Pxy Slide Panning Commands */
                        UniEffect(SharpMikCommon.Commands.UNI_ITEFFECTP, inf);
                        break;
                    case 0x11: /* Qxy Retrig (+volumeslide) */
                        m_Tracker.UniWriteByte((byte)SharpMikCommon.Commands.UNI_S3MEFFECTQ);
                        if (inf != 0 && lo == 0 && !((flags & SharpMikCommon.S3MIT_OLDSTYLE) == SharpMikCommon.S3MIT_OLDSTYLE))
                            m_Tracker.UniWriteByte(1);
                        else
                            m_Tracker.UniWriteByte(inf);
                        break;
                    case 0x12: /* Rxy tremolo speed x, depth y */
                        UniEffect(SharpMikCommon.Commands.UNI_S3MEFFECTR, inf);
                        break;
                    case 0x13: /* Sxx special commands */
                        if (inf >= 0xf0)
                        {
                            /* change resonant filter settings if necessary */
                            if ((filters) && ((inf & 0xf) != activemacro))
                            {
                                activemacro = (byte)(inf & 0xf);
                                for (inf = 0; inf < 0x80; inf++)
                                    filtersettings[inf].filter = filtermacros[activemacro];
                            }
                        }
                        else
                        {
                            /* Scream Tracker does not have samples larger than
							   64 Kb, thus doesn't need the SAx effect */
                            if ((flags & SharpMikCommon.S3MIT_SCREAM) == SharpMikCommon.S3MIT_SCREAM && ((inf & 0xf0) == 0xa0))
                                break;

                            UniEffect(SharpMikCommon.Commands.UNI_ITEFFECTS0, inf);
                        }
                        break;
                    case 0x14: /* Txx tempo */
                        if (inf >= 0x20)
                            UniEffect(SharpMikCommon.Commands.UNI_S3MEFFECTT, inf);
                        else
                        {
                            if (!((flags & SharpMikCommon.S3MIT_OLDSTYLE) == SharpMikCommon.S3MIT_OLDSTYLE))
                                /* IT Tempo slide */
                                UniEffect(SharpMikCommon.Commands.UNI_ITEFFECTT, inf);
                        }
                        break;
                    case 0x15: /* Uxy Fine Vibrato speed x, depth y */
                        if ((flags & SharpMikCommon.S3MIT_OLDSTYLE) == SharpMikCommon.S3MIT_OLDSTYLE)
                            UniEffect(SharpMikCommon.Commands.UNI_S3MEFFECTU, inf);
                        else
                            UniEffect(SharpMikCommon.Commands.UNI_ITEFFECTU, inf);
                        break;
                    case 0x16: /* Vxx Set Global Volume */
                        UniEffect(SharpMikCommon.Commands.UNI_XMEFFECTG, inf);
                        break;
                    case 0x17: /* Wxy Global Volume Slide */
                        UniEffect(SharpMikCommon.Commands.UNI_ITEFFECTW, inf);
                        break;
                    case 0x18: /* Xxx amiga command 8xx */
                        if ((flags & SharpMikCommon.S3MIT_OLDSTYLE) == SharpMikCommon.S3MIT_OLDSTYLE)
                        {
                            if (inf > 128)
                                UniEffect(SharpMikCommon.Commands.UNI_ITEFFECTS0, 0x91); /* surround */
                            else
                                UniPTEffect(0x8, (inf == 128) ? 255 : (inf << 1));
                        }
                        else
                            UniPTEffect(0x8, inf);
                        break;
                    case 0x19: /* Yxy Panbrello  speed x, depth y */
                        UniEffect(SharpMikCommon.Commands.UNI_ITEFFECTY, inf);
                        break;
                    case 0x1a: /* Zxx midi/resonant filters */
                        if (filtersettings[inf].filter != 0)
                        {
                            m_Tracker.UniWriteByte((byte)(SharpMikCommon.Commands.UNI_ITEFFECTZ));
                            m_Tracker.UniWriteByte(filtersettings[inf].filter);
                            m_Tracker.UniWriteByte(filtersettings[inf].inf);
                        }
                        break;
                }
            }
        }

        protected bool ReadComment(ushort len)
        {
            if (len != 0)
            {
                int i;

                char[] comment = new char[len + 1];
                m_Reader.Read_bytes(comment, len);


                /* translate IT linefeeds */
                for (i = 0; i < len; i++)
                    if (comment[i] == '\r') comment[i] = '\n';

                comment[len] = (char)0; /* just in case */

                m_Module.comment = new string(comment);
            }

            return true;
        }

        protected void AllocLinear()
        {
            if (m_Module.numsmp > noteindexcount)
            {
                noteindexcount = m_Module.numsmp;

                if (noteindex == null)
                {
                    noteindex = new int[noteindexcount];
                }
                else
                {
                    Array.Resize(ref noteindex, noteindexcount);
                }
            }
        }

        protected int speed_to_finetune(uint speed, int sample)
        {
            int ctmp = 0, tmp, note = 1, finetune = 0;

            speed >>= 1;

            while ((tmp = (int)(ModPlayer.getfrequency(m_Module.flags, (uint)ModPlayer.getlinearperiod((ushort)(note << 1), (uint)0)))) < speed)
            {
                ctmp = tmp;
                note++;
            }

            if (tmp != speed)
            {
                if ((tmp - speed) < (speed - ctmp))
                    while (tmp > speed)
                        tmp = (int)(ModPlayer.getfrequency(m_Module.flags, (uint)ModPlayer.getlinearperiod((ushort)(note << 1), (uint)(--finetune))));
                else
                {
                    note--;
                    while (ctmp < speed)
                        ctmp = (int)(ModPlayer.getfrequency(m_Module.flags, (uint)ModPlayer.getlinearperiod((ushort)(note << 1), (uint)(++finetune))));
                }
            }

            noteindex[sample] = note - 4 * SharpMikCommon.Octave;
            return finetune;
        }
        #endregion


        #region unistream functions

        protected void UniReset()
        {
            m_Tracker.UniReset();
        }

        protected void UniNewline()
        {
            m_Tracker.UniNewline();
        }

        protected byte[] UniDup()
        {
            return m_Tracker.UniDup();
        }

        protected void UniWriteByte(int command)
        {
            m_Tracker.UniWriteByte((byte)command);
        }

        protected void UniWriteByte(SharpMikCommon.Commands command)
        {
            m_Tracker.UniWriteByte((byte)command);
        }


        protected void UniInstrument(int instrument)
        {
            UniEffect(SharpMikCommon.Commands.UNI_INSTRUMENT, instrument);
        }

        protected void UniNote(int note)
        {
            UniEffect(SharpMikCommon.Commands.UNI_NOTE, note);
        }

        public void UniPTEffect(int eff, int dat)
        {
            if (eff >= 0x10)
            {
                Console.WriteLine("UniPTEffect called with incorrect eff value {0}", eff);
            }
            else
            {
                if ((eff != 0) || (dat != 0) || (m_Module.flags & SharpMikCommon.UF_ARPMEM) == SharpMikCommon.UF_ARPMEM)
                {
                    UniEffect((ushort)(SharpMikCommon.Commands.UNI_PTEFFECT0 + eff), (byte)dat);
                }
            }
        }


        public void UniEffect(SharpMikCommon.Commands command, int dat)
        {
            UniEffect((int)command, dat);
        }

        /* Generic effect writing routine */
        public void UniEffect(int eff, int dat)
        {
            if (eff == 0 || (eff >= (ushort)SharpMikCommon.Commands.UNI_LAST))
            {
                return;
            }

            m_Tracker.UniWriteByte((byte)eff);

            if (munitrk.unioperands[eff] == 2)
            {
                m_Tracker.UniWriteWord((ushort)dat);
            }
            else
            {
                m_Tracker.UniWriteByte((byte)dat);
            }
        }

        /* Appends UNI_VOLEFFECT + effect/dat to unistream. */
        protected void UniVolEffect(ushort eff, byte dat)
        {
            if ((eff != 0) || (dat != 0))
            {
                m_Tracker.UniWriteByte((byte)SharpMikCommon.Commands.UNI_VOLEFFECTS);
                m_Tracker.UniWriteByte((byte)eff);
                m_Tracker.UniWriteByte(dat);
            }
        }

        /* Appends UNI_VOLEFFECT + effect/dat to unistream. */
        protected void UniVolEffect(SharpMikCommon.ITColumnEffect effect, int data)
        {
            ushort eff = (ushort)effect;
            byte dat = (byte)data;
            if ((eff != 0) || (dat != 0))
            {
                m_Tracker.UniWriteByte((byte)SharpMikCommon.Commands.UNI_VOLEFFECTS);
                m_Tracker.UniWriteByte((byte)eff);
                m_Tracker.UniWriteByte(dat);
            }
        }
        #endregion


        #region Helper Functions
        protected bool ReadLinedComment(ushort len, ushort linelen)
        {
            char[] tempcomment;
            //char[] line;
            char[] storage;
            ushort total = 0, t, lines;
            int i;
            int j = 0;

            lines = (ushort)((len + linelen - 1) / linelen);

            if (len != 0)
            {
                tempcomment = new char[len + 1];
                storage = new char[len + 1];

                for (j = 0; j < len; j++)
                {
                    tempcomment[j] = ' ';
                }

                m_Reader.Read_bytes(tempcomment, len);

                for (j = 0, total = t = 0; t < lines; t++, j += linelen)
                {
                    for (i = linelen; j + i < len && (i >= 0) && (tempcomment[j + i] == ' '); i--)
                        tempcomment[j + i] = (char)0;

                    for (i = 0; i < linelen; i++)
                        if (tempcomment[j + i] == 0)
                            break;

                    total += (ushort)(1 + i);
                }

                if (total > lines)
                {
                    m_Module.comment = "";
                    /* convert message */
                    for (j = t = 0; t < lines; t++, j += linelen)
                    {
                        for (i = 0; i < linelen; i++)
                            if ((storage[i] = tempcomment[j + i]) == 0)
                                break;

                        storage[i] = (char)0; /* if (i==linelen) */
                        m_Module.comment += new string(storage);
                        m_Module.comment += "\r";
                    }
                    storage = null;
                    tempcomment = null;
                }
            }

            return true;
        }
        #endregion


        #region abstract functions
        public abstract bool Init();
        public abstract bool Test();
        public abstract bool Load(int curious);
        public abstract void Cleanup();
        public abstract string LoadTitle();
        #endregion
    }
}

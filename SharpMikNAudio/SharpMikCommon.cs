using System;
using SharpMik.IO;
using System.Reflection;
using System.Linq;
using SharpMik.Interfaces;
using SharpMik.Attributes;
using System.Collections.Generic;

namespace SharpMik
{


	/*
	 * This file must be split up into multiple files each one having just one of these classes.
	 * 
	 * possibly better handling of all the common variables could be in order.
	 * 
	 */

	/*
	 * MikMod variable sizes and replaced with.
	 * 
	 * MikMod		C#			Size
	 * ------------------------------
	 * SBYTE		sbyte		1 byte signed
	 * UBYTE		byte		1 byte unsigned
	 * SWORD		short		2 byte signed
	 * UWORD		ushort		2 byte unsigned
	 * SLONG		int			4 byte signed
	 * ULONG		uint		4 byte unsigned
	 * SLONGLONG	long		8 byte signed
	 * ULONGLONG	ulong		8 byte unsigned
	 * 
	 */


	public class FILTER 
	{
		public byte filter;
		public byte inf;

		public void Clear()
		{
			filter = 0;
			inf = 0;
		}
	};

	public class EnvPt
	{
		public short pos;
		public short val;
	}

	public class ENVPR
	{
		public byte flg;          /* envelope flag */
		public byte pts;          /* number of envelope points */
		public byte susbeg;       /* envelope sustain index begin */
		public byte susend;       /* envelope sustain index end */
		public byte beg;          /* envelope loop begin */
		public byte end;          /* envelope loop end */
		public short p;            /* current envelope counter */
		public ushort a;            /* envelope index a */
		public ushort b;            /* envelope index b */
		public EnvPt[]? env;          /* envelope points */
	}


	public class MP_CHANNEL
	{
		public INSTRUMENT? i;
		public SAMPLE? s;
		public byte sample;       /* which sample number */
		public byte note;         /* the audible note as heard, direct rep of period */
		public short outvolume;    /* output volume (vol + sampcol + instvol) */
		public sbyte chanvol;      /* channel's "global" volume */
		public ushort fadevol;      /* fading volume rate */
		public short panning;      /* panning position */
		public byte kick;         /* if true = sample has to be restarted */
		public byte kick_flag;   /* kick has been true */
		public ushort period;       /* period to play the sample at */
		public byte nna;          /* New note action type + master/slave flags */

		public byte volflg;       /* volume envelope settings */
		public byte panflg;       /* panning envelope settings */
		public byte pitflg;       /* pitch envelope settings */

		public byte keyoff;       /* if true = fade out and stuff */
		public short handle;       /* which sample-handle */
		public byte notedelay;    /* (used for note delay) */
		public long start;        /* The starting byte index in the sample */

		public MP_CHANNEL Clone()
		{
			return (MP_CHANNEL)this.MemberwiseClone();
		}

		public void CloneTo(MP_CHANNEL chan)
		{
			chan.i = i;
			chan.s = s;
			chan.sample = sample;
			chan.note = note;
			chan.outvolume = outvolume;
			chan.chanvol = chanvol;
			chan.fadevol = fadevol;
			chan.panning = panning;
			chan.kick = kick;
			chan.period = period;
			chan.nna = nna;

			chan.volflg = volflg;
			chan.panflg = panflg;
			chan.pitflg = pitflg;

			chan.keyoff = keyoff;
			chan.handle = handle;
			chan.notedelay = notedelay;
			chan.start = start;
		}
	};


	public class SAMPLE 
	{		 
		public short  panning;     /* panning (0-255 or PAN_SURROUND) */
		public uint  speed;       /* Base playing speed/frequency of note */
		public byte  volume;      /* volume 0-64 */
		public ushort  inflags;		/* sample format on disk */
		public ushort  flags;       /* sample format in memory */
		public uint  length;      /* length of sample (in samples!) */
		public uint  loopstart;   /* repeat position (relative to start, in samples) */
		public uint  loopend;     /* repeat end */
		public uint  susbegin;    /* sustain loop begin (in samples) \  Not Supported */
		public uint  susend;      /* sustain loop end                /      Yet! */

		/* Variables used by the module player only! (ignored for sound effects) */
		public byte  globvol;     /* global volume */
		public byte  vibflags;    /* autovibrato flag stuffs */
		public byte  vibtype;     /* Vibratos moved from INSTRUMENT to SAMPLE */
		public byte  vibsweep;
		public byte  vibdepth;
		public byte  vibrate;
		public string? samplename;  /* name of the sample */

		/* Values used internally only */
		public ushort  avibpos;     /* autovibrato pos [player use] */
		public byte  divfactor;   /* for sample scaling, maintains proper period slides */
		public uint  seekpos;     /* seek position in file */
		public short  handle;      /* sample handle used by individual drivers */
	} 



	public class INSTRUMENT 
	{
		public INSTRUMENT()
		{
			samplenumber = new ushort[SharpMikCommon.INSTNOTES];
			samplenote = new byte[SharpMikCommon.INSTNOTES];
			volenv = new EnvPt[SharpMikCommon.ENVPOINTS];
			panenv = new EnvPt[SharpMikCommon.ENVPOINTS];
			pitenv = new EnvPt[SharpMikCommon.ENVPOINTS];

			for (int i = 0; i < SharpMikCommon.ENVPOINTS; i++)
			{
				volenv[i] = new EnvPt();
				panenv[i] = new EnvPt();
				pitenv[i] = new EnvPt();
			}
		}

		public string insname;

		public byte		flags;
		public ushort []	samplenumber; // INSTNOTES
		public byte[] samplenote; // INSTNOTES

		public byte nnatype;
		public byte dca;              /* duplicate check action */
		public byte dct;              /* duplicate check type */
		public byte globvol;
		public ushort volfade;
		public short panning;          /* instrument-based panning var */

		public byte pitpansep;        /* pitch pan separation (0 to 255) */
		public byte pitpancenter;     /* pitch pan center (0 to 119) */
		public byte rvolvar;          /* random volume varations (0 - 100%) */
		public byte rpanvar;          /* random panning varations (0 - 100%) */

		/* volume envelope */
		public byte volflg;           /* bit 0: on 1: sustain 2: loop */
		public byte volpts;
		public byte volsusbeg;
		public byte volsusend;
		public byte volbeg;
		public byte volend;
		public EnvPt[] volenv; // ENVPOINTS
		/* panning envelope */
		public byte panflg;           /* bit 0: on 1: sustain 2: loop */
		public byte panpts;
		public byte pansusbeg;
		public byte pansusend;
		public byte panbeg;
		public byte panend;
		public EnvPt[] panenv; // ENVPOINTS
		/* pitch envelope */
		public byte pitflg;           /* bit 0: on 1: sustain 2: loop */
		public byte pitpts;
		public byte pitsusbeg;
		public byte pitsusend;
		public byte pitbeg;
		public byte pitend;
		public EnvPt[] pitenv; // ENVPOINTS
	}


	public class MP_CONTROL 
	{
		public MP_CONTROL()
		{
			main = new MP_CHANNEL();
			slave = null;
		}

		public MP_CHANNEL	main;

		public MP_VOICE	slave;	  /* Audio Slave of current effects control channel */

		public byte       slavechn;     /* Audio Slave of current effects control channel */
		public byte       muted;        /* if set, channel not played */
		public ushort		ultoffset;    /* fine sample offset memory */
		public byte       anote;        /* the note that indexes the audible */
		public byte		oldnote;
		public short       ownper;
		public short       ownvol;
		public byte       dca;          /* duplicate check action */
		public byte       dct;          /* duplicate check type */
		public byte[]      row;          /* row currently playing on this channel */
		public int        rowPos = 0;
		public sbyte       retrig;       /* retrig value (0 means don't retrig) */
		public uint       speed;        /* what finetune to use */
		public short       volume;       /* amiga volume (0 t/m 64) to play the sample at */

		public short       tmpvolume;    /* tmp volume */
		public ushort       tmpperiod;    /* tmp period */
		public ushort       wantedperiod; /* period to slide to (with effect 3 or 5) */

		public byte       arpmem;       /* arpeggio command memory */
		public byte       pansspd;      /* panslide speed */
		public ushort       slidespeed;
		public ushort       portspeed;    /* noteslide speed (toneportamento) */

		public byte       s3mtremor;    /* s3m tremor (effect I) counter */
		public byte       s3mtronof;    /* s3m tremor ontime/offtime */
		public byte       s3mvolslide;  /* last used volslide */
		public sbyte       sliding;
		public byte       s3mrtgspeed;  /* last used retrig speed */
		public byte       s3mrtgslide;  /* last used retrig slide */

		public byte       glissando;    /* glissando (0 means off) */
		public byte       wavecontrol;

		public sbyte       vibpos;       /* current vibrato position */
		public byte       vibspd;       /* "" speed */
		public byte       vibdepth;     /* "" depth */

		public sbyte       trmpos;       /* current tremolo position */
		public byte       trmspd;       /* "" speed */
		public byte       trmdepth;     /* "" depth */

		public byte       fslideupspd;
		public byte       fslidednspd;
		public byte       fportupspd;   /* fx E1 (extra fine portamento up) data */
		public byte       fportdnspd;   /* fx E2 (extra fine portamento dn) data */
		public byte       ffportupspd;  /* fx X1 (extra fine portamento up) data */
		public byte       ffportdnspd;  /* fx X2 (extra fine portamento dn) data */

		public uint       hioffset;     /* last used high order of sample offset */
		public ushort       soffset;      /* last used low order of sample-offset (effect 9) */

		public byte       sseffect;     /* last used Sxx effect */
		public byte       ssdata;       /* last used Sxx data info */
		public byte       chanvolslide; /* last used channel volume slide */

		public byte       panbwave;     /* current panbrello waveform */
		public byte       panbpos;      /* current panbrello position */
		public sbyte       panbspd;      /* "" speed */
		public byte       panbdepth;    /* "" depth */

		public ushort       newsamp;      /* set to 1 upon a sample / inst change */
		public byte       voleffect;    /* Volume Column Effect Memory as used by IT */
		public byte       voldata;      /* Volume Column Data Memory */

		public short       pat_reppos;   /* patternloop position */
		public ushort       pat_repcnt;   /* times to loop */
	} ;

/* Used by NNA only player (audio control.  AUDTMP is used for full effects
   control). */
	public class MP_VOICE 
	{
		public MP_VOICE()
		{
			venv = new ENVPR();
			penv = new ENVPR();
			cenv = new ENVPR();

			main = new MP_CHANNEL();
		}

		public MP_CHANNEL	main;

		public ENVPR       venv;
		public ENVPR       penv;
		public ENVPR       cenv;

		public ushort       avibpos;      /* autovibrato pos */
		public ushort       aswppos;      /* autovibrato sweep pos */

		public uint       totalvol;     /* total volume of channel (before global mixings) */

		public bool        mflag;
		public short       masterchn;
		public ushort       masterperiod;

		public MP_CONTROL master;       /* index of "master" effects channel */
	} 



	public class MikModule 
	{
		public MikModule()
		{
			panning = new ushort[SharpMikCommon.UF_MAXCHAN];
			chanvol = new byte[SharpMikCommon.UF_MAXCHAN];			
		}


	/* general module information */
		public string songname;    /* name of the song */
		public string modtype;     /* string type of module loaded */
		public string comment;     /* module comments */

		public ushort       flags;       /* See module flags above */
		public byte       numchn;      /* number of module channels */
		public byte       numvoices;   /* max # voices used for full NNA playback */
		public ushort       numpos;      /* number of positions in this song */
		public ushort       numpat;      /* number of patterns in this song */
		public ushort       numins;      /* number of instruments */
		public ushort       numsmp;      /* number of samples */
		public INSTRUMENT[] instruments; /* all instruments */
		public SAMPLE[]     samples;     /* all samples */
		public byte       realchn;     /* real number of channels used */
		public byte       totalchn;    /* total number of channels used (incl NNAs) */

	/* playback settings */
		public ushort       reppos;      /* restart position */
		public byte       initspeed;   /* initial song speed */
		public ushort       inittempo;   /* initial song tempo */
		public byte       initvolume;  /* initial global volume (0 - 128) */
		public ushort[]       panning; /* panning positions */
		public byte[]       chanvol; /* channel positions */
		public ushort       bpm;         /* current beats-per-minute speed */
		public ushort       sngspd;      /* current song speed */
		public short       volume;      /* song volume (0-128) (or user volume) */

		public bool        extspd;      /* extended speed flag (default enabled) */
		public bool        panflag;     /* panning flag (default enabled) */
		public bool        wrap;        /* wrap module ? (default disabled) */
		public bool        loop;		 /* allow module to loop ? (default enabled) */
		public bool        fadeout;	 /* volume fade out during last pattern */

		public ushort       patpos;      /* current row number */
		public short       sngpos;      /* current song position */
		public uint       sngtime;     /* current song time in 2^-10 seconds */

		public short       relspd;      /* relative speed factor */

	/* internal module representation */
		public ushort       numtrk;      /* number of tracks */
		public byte[][]     tracks;      /* array of numtrk pointers to tracks */
		public ushort[]      patterns;    /* array of Patterns */
		public ushort[]      pattrows;    /* array of number of rows for each pattern */
		public ushort[]      positions;   /* all positions */

		public bool        forbid;      /* if true, no player update! */
		public ushort       numrow;      /* number of rows on current pattern */
		public ushort       vbtick;      /* tick counter (counts from 0 to sngspd) */
		public ushort       sngremainder;/* used for song time computation */

		public MP_CONTROL[]  control;     /* Effects Channel info (size pf.numchn) */
		public MP_VOICE[]    voice;       /* Audio Voice information (size md_numchn) */

		public byte       globalslide; /* global volume slide rate */
		public byte       pat_repcrazy;/* module has just looped to position -1 */
		public ushort       patbrk;      /* position where to start a new pattern */
		public byte       patdly;      /* patterndelay counter (command memory) */
		public byte       patdly2;     /* patterndelay counter (real one) */
		public short       posjmp;      /* flag to indicate a jump is needed... */
		public ushort		bpmlimit;	 /* threshold to detect bpm or speed values */


		public void AllocSamples()
		{
			samples = new SAMPLE[numsmp];

			for (int i=0;i<numsmp;i++)
			{
				samples[i] = new SAMPLE();
				samples[i].panning = 128;
				samples[i].handle = -1;
				samples[i].globvol = 64;
				samples[i].volume = 64;
			}			
		}

		public void AllocPatterns()
		{
			ushort tracks = 0;
			patterns = new ushort[(numpat + 1) * numchn];
			pattrows = new ushort[numpat + 1];

			for(int t=0;t<=numpat;t++) 
			{
				pattrows[t]=64;
				for(int s=0;s<numchn;s++)
				{
					patterns[(t*numchn)+s]=tracks++;
				}
			}	
		}


		public void AllocTracks()
		{
			tracks = new byte[numtrk][];
		}

		public bool AllocInstruments()
		{
			if (numins == 0)
			{
				return false;
			}

			instruments = new INSTRUMENT[numins];

			for (ushort i = 0; i < instruments.Length;i++ )
			{
				instruments[i] = new INSTRUMENT();
				for(byte n=0;n<SharpMikCommon.INSTNOTES;n++) 
				{
					instruments[i].samplenote[n] = n;
					instruments[i].samplenumber[n] = i;
				}
				
				instruments[i].globvol = 64;
			}

			return true;
		}
	}


	/*========== Samples */

/* This is a handle of sorts attached to any sample registered with
   SL_RegisterSample.  Generally, this only need be used or changed by the
   loaders and drivers of mikmod. */
	public class SAMPLOAD 
	{
		public SAMPLOAD next;

		public uint length;       /* length of sample (in samples!) */
		public uint loopstart;    /* repeat position (relative to start, in samples) */
		public uint loopend;      /* repeat end */
		public uint infmt, outfmt;
		public int scalefactor;
		public SAMPLE sample;
		public ModuleReader reader;
	}

    /* This structure is used to query current playing voices status */
    public class VOICEINFO
    {
        public INSTRUMENT i;            /* Current channel instrument */
        public SAMPLE s;            /* Current channel sample */
        public short panning;      /* panning position */
        public sbyte volume;       /* channel's "global" volume (0..64) */
        public ushort period;       /* period to play the sample at */
        public byte kick;         /* if true = sample has been restarted */
    }




    public class SharpMikCommon
	{
		#region enums
		public enum Commands 
		{
			/* Simple note */
			UNI_NOTE = 1,
			/* Instrument change */
			UNI_INSTRUMENT,
			/* Protracker effects */
			UNI_PTEFFECT0,     /* arpeggio */
			UNI_PTEFFECT1,     /* porta up */
			UNI_PTEFFECT2,     /* porta down */
			UNI_PTEFFECT3,     /* porta to note */
			UNI_PTEFFECT4,     /* vibrato */
			UNI_PTEFFECT5,     /* dual effect 3+A */
			UNI_PTEFFECT6,     /* dual effect 4+A */
			UNI_PTEFFECT7,     /* tremolo */
			UNI_PTEFFECT8,     /* pan */
			UNI_PTEFFECT9,     /* sample offset */
			UNI_PTEFFECTA,     /* volume slide */
			UNI_PTEFFECTB,     /* pattern jump */
			UNI_PTEFFECTC,     /* set volume */
			UNI_PTEFFECTD,     /* pattern break */
			UNI_PTEFFECTE,     /* extended effects */
			UNI_PTEFFECTF,     /* set speed */
			/* Scream Tracker effects */
			UNI_S3MEFFECTA,    /* set speed */
			UNI_S3MEFFECTD,    /* volume slide */
			UNI_S3MEFFECTE,    /* porta down */
			UNI_S3MEFFECTF,    /* porta up */
			UNI_S3MEFFECTI,    /* tremor */
			UNI_S3MEFFECTQ,    /* retrig */
			UNI_S3MEFFECTR,    /* tremolo */
			UNI_S3MEFFECTT,    /* set tempo */
			UNI_S3MEFFECTU,    /* fine vibrato */
			UNI_KEYOFF,        /* note off */
			/* Fast Tracker effects */
			UNI_KEYFADE,       /* note fade */
			UNI_VOLEFFECTS,    /* volume column effects */
			UNI_XMEFFECT4,     /* vibrato */
			UNI_XMEFFECT6,     /* dual effect 4+A */
			UNI_XMEFFECTA,     /* volume slide */
			UNI_XMEFFECTE1,    /* fine porta up */
			UNI_XMEFFECTE2,    /* fine porta down */
			UNI_XMEFFECTEA,    /* fine volume slide up */
			UNI_XMEFFECTEB,    /* fine volume slide down */
			UNI_XMEFFECTG,     /* set global volume */
			UNI_XMEFFECTH,     /* global volume slide */
			UNI_XMEFFECTL,     /* set envelope position */
			UNI_XMEFFECTP,     /* pan slide */
			UNI_XMEFFECTX1,    /* extra fine porta up */
			UNI_XMEFFECTX2,    /* extra fine porta down */
			/* Impulse Tracker effects */
			UNI_ITEFFECTG,     /* porta to note */
			UNI_ITEFFECTH,     /* vibrato */
			UNI_ITEFFECTI,     /* tremor (xy not incremented) */
			UNI_ITEFFECTM,     /* set channel volume */
			UNI_ITEFFECTN,     /* slide / fineslide channel volume */
			UNI_ITEFFECTP,     /* slide / fineslide channel panning */
			UNI_ITEFFECTT,     /* slide tempo */
			UNI_ITEFFECTU,     /* fine vibrato */
			UNI_ITEFFECTW,     /* slide / fineslide global volume */
			UNI_ITEFFECTY,     /* panbrello */
			UNI_ITEFFECTZ,     /* resonant filters */
			UNI_ITEFFECTS0,
			/* UltraTracker effects */
			UNI_ULTEFFECT9,    /* Sample fine offset */
			/* OctaMED effects */
			UNI_MEDSPEED,
			UNI_MEDEFFECTF1,   /* play note twice */
			UNI_MEDEFFECTF2,   /* delay note */
			UNI_MEDEFFECTF3,   /* play note three times */
			/* Oktalyzer effects */
			UNI_OKTARP,		   /* arpeggio */

			UNI_LAST
		};

		public enum ExtentedEffects 
		{
			SS_GLISSANDO = 1,
			SS_FINETUNE,
			SS_VIBWAVE,
			SS_TREMWAVE,
			SS_PANWAVE,
			SS_FRAMEDELAY,
			SS_S7EFFECTS,
			SS_PANNING,
			SS_SURROUND,
			SS_HIOFFSET,
			SS_PATLOOP,
			SS_NOTECUT,
			SS_NOTEDELAY,
			SS_PATDELAY
		};


		public enum MDTypes
		{
			MD_MUSIC = 0,
			MD_SNDFX
		};

		public enum MDDecodeTypes
		{
			MD_HARDWARE = 0,
			MD_SOFTWARE
		};


		/* IT Volume column effects */
		public enum ITColumnEffect
        {
			VOL_VOLUME = 1,
			VOL_PANNING,
			VOL_VOLSLIDE,
			VOL_PITCHSLIDEDN,
			VOL_PITCHSLIDEUP,
			VOL_PORTAMENTO,
			VOL_VIBRATO
		};

		public enum MuteOptions
		{
			MuteRangeInclusive,
			MuteRangeExclusive,
			MuteList,
			MuteAll,
		};
		#endregion


		#region consts







		public const int Octave = 12;


		public const int	UF_MAXMACRO		=	0x10;
		public const int	UF_MAXFILTER	=	0x100;

		public const int	FILT_CUT		=	0x80;
		public const int	FILT_RESONANT	=	0x81;

		/* flags for S3MIT_ProcessCmd */
		public const int	S3MIT_OLDSTYLE	=	1;	/* behave as old scream tracker */
		public const int	S3MIT_IT		=	2;	/* behave as impulse tracker */
		public const int	S3MIT_SCREAM	=	4;	/* enforce scream tracker specific limits */


		/*========== Instruments */

		/* Instrument format flags */
		public const int  IF_OWNPAN			=	1;
		public const int  IF_PITCHPAN		=	2;

		/* Envelope flags: */
		public const int  EF_ON				=	1;
		public const int  EF_SUSTAIN		=	2;
		public const int  EF_LOOP			=	4;
		public const int  EF_VOLENV			=	8;

		/* New Note Action Flags */
		public const int  NNA_CUT			=	0;
		public const int  NNA_CONTINUE		=	1;
		public const int  NNA_OFF			=	2;
		public const int  NNA_FADE			=	3;

		public const int  NNA_MASK			=	3;

		public const int  DCT_OFF			=	0;
		public const int  DCT_NOTE			=	1;
		public const int  DCT_SAMPLE		=	2;
		public const int  DCT_INST			=	3;

		public const int  DCA_CUT			=	0;
		public const int  DCA_OFF			=	1;
		public const int  DCA_FADE			=	2;

		public const int  KEY_KICK			=	0;
		public const int  KEY_OFF			=	1;
		public const int  KEY_FADE			=	2;
		public const int  KEY_KILL			=	(KEY_OFF|KEY_FADE);

		public const int  KICK_ABSENT		=	0;
		public const int  KICK_NOTE			=	1;
		public const int  KICK_KEYOFF		=	2;
		public const int  KICK_ENV			=	4;

		public const int  AV_IT				=	1;   /* IT vs. XM vibrato info */

		public const int UF_MAXCHAN			=	64;

		/* Sample format [loading and in-memory] flags: */
		public const int  SF_16BITS			=	0x0001;
		public const int  SF_STEREO			=	0x0002;
		public const int  SF_SIGNED			=	0x0004;
		public const int  SF_BIG_ENDIAN		=	0x0008;
		public const int  SF_DELTA			=	0x0010;
		public const int  SF_ITPACKED		=	0x0020;

		public const int 	SF_FORMATMASK	=	0x003F;

		/* General Playback flags */

		public const int  SF_LOOP			=	0x0100;
		public const int  SF_BIDI			=	0x0200;
		public const int  SF_REVERSE		=	0x0400;
		public const int  SF_SUSTAIN		=	0x0800;

		public const int  SF_PLAYBACKMASK	=	0x0C00;


		/*========== Playing */

		public const int  POS_NONE			= (-2);	/* no loop position defined */

		public const int  LAST_PATTERN		= ushort.MaxValue; 	/* (ushort)-1 special ``end of song'' pattern */

		public const int INSTNOTES			=	120;
		public const int ENVPOINTS			=	32;


		/* Module flags */
		public const int UF_XMPERIODS		=	0x0001; /* XM periods / finetuning */
		public const int UF_LINEAR			=	0x0002; /* LINEAR periods (UF_XMPERIODS must be set) */
		public const int UF_INST			=	0x0004; /* Instruments are used */
		public const int UF_NNA				=	0x0008; /* IT: NNA used, set numvoices rather
															than numchn */
		public const int UF_S3MSLIDES		=	0x0010; /* uses old S3M volume slides */
		public const int UF_BGSLIDES		=	0x0020; /* continue volume slides in the background */
		public const int UF_HIGHBPM			=	0x0040; /* MED: can use >255 bpm */
		public const int UF_NOWRAP			=	0x0080; /* XM-type (i.e. illogical) pattern break
														semantics */
		public const int UF_ARPMEM			=	0x0100; /* IT: need arpeggio memory */
		public const int UF_FT2QUIRKS		=	0x0200;/* emulate some FT2 replay quirks */
		public const int UF_PANNING			=	0x0400; /* module uses panning effects or have non-tracker default initial panning */

		/* Panning constants */
		public const int PAN_LEFT			=	0;
		public const int PAN_HALFLEFT 		=	64;
		public const int PAN_CENTER			=	128;
		public const int PAN_HALFRIGHT		=	192;
		public const int PAN_RIGHT			=	255;
		public const int PAN_SURROUND		=	512; /* panning value for Dolby Surround */



		/* These ones take effect only after MikMod_Init or MikMod_Reset */
		public const int DMODE_16BITS		=	0x0001; /* enable 16 bit output */
		public const int DMODE_STEREO		=	0x0002; /* enable stereo output */
		public const int DMODE_SOFT_SNDFX	=	0x0004; /* Process sound effects via software mixer */
		public const int DMODE_SOFT_MUSIC	=	0x0008; /* Process music via software mixer */
		public const int DMODE_HQMIXER		=	0x0010; /* Use high-quality (slower) software mixer */
		/* These take effect immediately. */
		public const int DMODE_SURROUND		=	0x0100; /* enable surround sound */
		public const int DMODE_INTERP		=	0x0200; /* enable interpolation */
		public const int DMODE_REVERSE		=	0x0400; /* reverse stereo */
        public const int DMODE_NOISEREDUCTION = 0x1000; /* Low pass filtering */

        /* Module-only Playback Flags */

        public const int SF_OWNPAN		= 0x1000;
		public const int SF_UST_LOOP    = 0x2000;

		public const int SF_EXTRAPLAYBACKMASK	= 0x3000;		

		public static short[] Npertab = 
		{
			/* Octaves 6 . 0 */
			/* C    C#     D    D#     E     F    F#     G    G#     A    A#     B */
			0x6b0,0x650,0x5f4,0x5a0,0x54c,0x500,0x4b8,0x474,0x434,0x3f8,0x3c0,0x38a,
			0x358,0x328,0x2fa,0x2d0,0x2a6,0x280,0x25c,0x23a,0x21a,0x1fc,0x1e0,0x1c5,
			0x1ac,0x194,0x17d,0x168,0x153,0x140,0x12e,0x11d,0x10d,0x0fe,0x0f0,0x0e2,
			0x0d6,0x0ca,0x0be,0x0b4,0x0aa,0x0a0,0x097,0x08f,0x087,0x07f,0x078,0x071,
			0x06b,0x065,0x05f,0x05a,0x055,0x050,0x04b,0x047,0x043,0x03f,0x03c,0x038,
			0x035,0x032,0x02f,0x02d,0x02a,0x028,0x025,0x023,0x021,0x01f,0x01e,0x01c,
			0x01b,0x019,0x018,0x016,0x015,0x014,0x013,0x012,0x011,0x010,0x00f,0x00e
		};

		public static ushort[] finetune =
		{
			8363,8413,8463,8529,8581,8651,8723,8757,
			7895,7941,7985,8046,8107,8169,8232,8280
		};
        #endregion



        #region Helper functions

        private static string[] s_FileTypes;

        public static string[] ModFileExtentions
        {
            get
            {
                if (s_FileTypes == null)
                {
                    var extentions = new List<string>();
                    var list = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsSubclassOf(typeof(IModLoader)));

                    foreach (var item in list)
                    {
                        var attributes = item.GetCustomAttributes(typeof(ModFileExtentionsAttribute),false);                                             
                        foreach (var attribute in attributes)
                        {
                            var modExtention = attribute as ModFileExtentionsAttribute;

                            if (modExtention != null)
                            {
                                extentions.AddRange(modExtention.FileExtentions);
                            }
                        }
                    }

                    s_FileTypes = extentions.Distinct().ToArray();
                }

                return s_FileTypes;
            }
        }


		public static bool MatchesExtentions(string filename)
		{
			bool match = false;
			foreach (string ext in ModFileExtentions)
			{
				string tolower = filename.ToLower();

				if (tolower.StartsWith(ext + ".") || tolower.EndsWith("." + ext))
				{
					match = true;
					break;
				}
			}

			return match;
		}
		#endregion

	}
}

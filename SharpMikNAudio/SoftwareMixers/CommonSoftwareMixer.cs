using SharpMik.Player;

namespace SharpMik.SoftwareMixers
{
    abstract public class CommonSoftwareMixer
    {
        protected class VoiceInfo
        {
            public byte Kick;                   // =1 -> sample has to be restarted
            public byte Active;                 // =1 -> sample is playing
            public ushort Flags;                // 16/8 bits looping/one-shot
            public short Handle;                // identifies the sample
            public uint Start;                  // start index
            public uint Size;                   // sample size
            public uint RepeatStartPosition;    // loop start
            public uint RepeatEndPosition;      // loop end
            public uint Frequency;              // current frequency
            public int Volume;                  // current volume
            public int Panning;                 // current panning position

            public int Click;
            public int LastValueLeft;
            public int LastValueRight;

            public int RampVolume;
            public int LeftVolumeFactor;
            public int RightVolumeFactor;       // Volume factor in range 0-255
            public int LeftVolumeOld;
            public int RightVolumeOld;

            public long CurrentSampleIndex;     // current index in the sample
            public long CurrentIncrement;       // increment value
        }


        protected const int TICKLSIZE = 8192;
        protected const int REVERBERATION = 110000;




        protected List<short[]> m_Samples;
        protected VoiceInfo[]? m_VoiceInfos = null;
        protected VoiceInfo? m_CurrentVoiceInfo = null;
        protected long m_TickLeft = 0;
        protected long m_SamplesThatFit = 0;
        protected long m_VcMemory = 0;
        protected int m_VcSoftChannel;
        protected long m_IdxSize;
        protected long m_IdxlPos;
        protected long m_IdxlEnd;
        protected int[] m_VcTickBuf;
        protected ushort m_VcMode;

        protected bool m_IsStereo;

        // Reverb vars
        protected uint m_RvrIndex;
        protected int[][][]? m_RvBuf = null;
        protected int[] m_Rvc;



        protected int m_FracBits;
        protected int m_ClickBuffer;
        protected int m_ReverbMultipler;


        protected abstract bool MixerInit();
        protected abstract uint WriteSamples(sbyte[] buf, uint todo);


        public delegate void VC_CallbackDelegate(int[] buffer, int size);

        public event VC_CallbackDelegate VC_Callback;

        public bool Init()
        {
            MixerInit();

            m_Samples = new List<short[]>();

            m_VcTickBuf = new int[TICKLSIZE];

            m_VcMode = ModDriver.Mode;

            m_Rvc = new int[8];

            m_Rvc[0] = (5000 * ModDriver.MixFrequency) / (REVERBERATION * m_ReverbMultipler);
            m_Rvc[1] = (5078 * ModDriver.MixFrequency) / (REVERBERATION * m_ReverbMultipler);
            m_Rvc[2] = (5313 * ModDriver.MixFrequency) / (REVERBERATION * m_ReverbMultipler);
            m_Rvc[3] = (5703 * ModDriver.MixFrequency) / (REVERBERATION * m_ReverbMultipler);
            m_Rvc[4] = (6250 * ModDriver.MixFrequency) / (REVERBERATION * m_ReverbMultipler);
            m_Rvc[5] = (6953 * ModDriver.MixFrequency) / (REVERBERATION * m_ReverbMultipler);
            m_Rvc[6] = (7813 * ModDriver.MixFrequency) / (REVERBERATION * m_ReverbMultipler);
            m_Rvc[7] = (8828 * ModDriver.MixFrequency) / (REVERBERATION * m_ReverbMultipler);


            m_RvBuf = new int[2][][];
            for (int side = 0; side < 2; side++)
            {
                m_RvBuf[side] = new int[8][];
                for (int channel = 0; channel < 8; channel++)
                {
                    m_RvBuf[side][channel] = new int[m_Rvc[channel] + 1];
                }
            }
            m_IsStereo = (m_VcMode & SharpMikCommon.DMODE_STEREO) == SharpMikCommon.DMODE_STEREO;

            return false;
        }

        protected void FireCallBack(int portion)
        {
            if (VC_Callback != null)
            {
                VC_Callback(m_VcTickBuf, portion);
            }
        }

        public void DeInit()
        {
            m_VcTickBuf = null;
            m_VoiceInfos = null;
            m_Samples = null;
            m_RvBuf = null;
        }

        public bool PlayStart()
        {
            m_SamplesThatFit = TICKLSIZE;
            m_IsStereo = (m_VcMode & SharpMikCommon.DMODE_STEREO) == SharpMikCommon.DMODE_STEREO;

            if (m_IsStereo)
            {
                m_SamplesThatFit >>= 1;
            }

            m_TickLeft = 0;
            m_RvrIndex = 0;

            return false;
        }

        protected uint samples2bytes(uint samples)
        {
            if ((m_VcMode & SharpMikCommon.DMODE_16BITS) == SharpMikCommon.DMODE_16BITS)
            {
                samples <<= 1;
            }

            if (m_IsStereo)
            {
                samples <<= 1;
            }
            return samples;
        }


        protected uint bytes2samples(uint bytes)
        {
            if ((m_VcMode & SharpMikCommon.DMODE_16BITS) == SharpMikCommon.DMODE_16BITS)
            {
                bytes >>= 1;
            }

            if (m_IsStereo)
            {
                bytes >>= 1;
            }
            return bytes;
        }


        public bool SetNumVoices()
        {
            int t;

            if ((m_VcSoftChannel = ModDriver.SoftwareChannel) == 0)
            {
                return true;
            }

            if (m_VoiceInfos != null)
                m_VoiceInfos = null;

            m_VoiceInfos = new VoiceInfo[m_VcSoftChannel];

            for (t = 0; t < m_VcSoftChannel; t++)
            {
                m_VoiceInfos[t] = new VoiceInfo();

                m_VoiceInfos[t].Frequency = 10000;
                m_VoiceInfos[t].Panning = (t & 1) == 1 ? SharpMikCommon.PAN_LEFT : SharpMikCommon.PAN_RIGHT;
            }

            return false;
        }

        public void VoiceSetVolume(byte voice, ushort volume)
        {
            /* protect against clicks if volume variation is too high */
            if (Math.Abs((int)m_VoiceInfos[voice].Volume - (int)volume) > 32)
            {
                m_VoiceInfos[voice].RampVolume = m_ClickBuffer;
            }

            m_VoiceInfos[voice].Volume = volume;
        }

        public ushort VoiceGetVolume(byte voice)
        {
            return (ushort)m_VoiceInfos[voice].Volume;
        }

        public void VoiceSetFrequency(byte voice, uint freq)
        {
            m_VoiceInfos[voice].Frequency = freq;
        }

        public uint VoiceGetFrequency(byte voice)
        {
            return m_VoiceInfos[voice].Frequency;
        }

        public void VoiceSetPanning(byte voice, uint panning)
        {
            /* protect against clicks if panning variation is too high */
            if (Math.Abs((int)m_VoiceInfos[voice].Panning - (int)panning) > 48)
            {
                m_VoiceInfos[voice].RampVolume = m_ClickBuffer;
            }
            m_VoiceInfos[voice].Panning = (int)panning;
        }

        public uint VoiceGetPanning(byte voice)
        {
            return (uint)m_VoiceInfos[voice].Panning;
        }

        public void VoicePlay(byte voice, short handle, uint start, uint size, uint reppos, uint repend, ushort flags)
        {
            m_VoiceInfos[voice].Flags = flags;
            m_VoiceInfos[voice].Handle = handle;
            m_VoiceInfos[voice].Start = start;
            m_VoiceInfos[voice].Size = size;
            m_VoiceInfos[voice].RepeatStartPosition = reppos;
            m_VoiceInfos[voice].RepeatEndPosition = repend;
            m_VoiceInfos[voice].Kick = 1;
        }

        public void VoiceStop(byte voice)
        {
            m_VoiceInfos[voice].Active = 0;
        }

        public bool VoiceStopped(byte voice)
        {
            return (m_VoiceInfos[voice].Active == 0);
        }

        public int VoiceGetPosition(byte voice)
        {
            return (int)(m_VoiceInfos[voice].CurrentIncrement >> m_FracBits);
        }

        public uint VoiceRealVolume(byte voice)
        {
            int i, s, size;
            int k, j;
            int t;

            t = (int)(m_VoiceInfos[voice].CurrentIncrement >> m_FracBits);
            if (m_VoiceInfos[voice].Active == 0)
            {
                return 0;
            }

            s = m_VoiceInfos[voice].Handle;
            size = (int)m_VoiceInfos[voice].Size;

            i = 64; t -= 64; k = 0; j = 0;
            if (i > size) i = size;
            if (t < 0) t = 0;
            if (t + i > size) t = size - i;

            i &= ~1;  /* make sure it's EVEN. */

            int place = t;
            for (; i != 0; i--, place++)
            {
                if (k < m_Samples[s][place])
                {
                    k = m_Samples[s][place];
                }
                if (j > m_Samples[s][place])
                {
                    j = m_Samples[s][place];
                }
            }
            return (uint)Math.Abs(k - j);
        }




        public short SampleLoad(SAMPLOAD sload, int type)
        {
            SAMPLE s = sload.sample;

            int handle;
            uint t, length, loopstart, loopend;

            if (type == (int)SharpMikCommon.MDDecodeTypes.MD_HARDWARE)
            {
                return 0;
            }

            /* Find empty slot to put sample address in */
            for (handle = 0; handle < m_Samples.Count; handle++)
            {
                if (m_Samples[handle] == null)
                    break;
            }


            if (handle == m_Samples.Count)
            {
                m_Samples.Add(null);
            }

            /* Reality check for loop settings */
            if (s.loopend > s.length)
            {
                s.loopend = s.length;
            }

            if (s.loopstart >= s.loopend)
            {
                int flags = s.flags;
                flags &= ~SharpMikCommon.SF_LOOP;

                s.flags = (ushort)flags;
            }

            length = s.length;
            loopstart = s.loopstart;
            loopend = s.loopend;

            SampleLoader.SL_SampleSigned(sload);
            SampleLoader.SL_Sample8to16(sload);

            uint len = ((length + 20) << 1);
            m_Samples[handle] = new short[len];

            /* read sample into buffer */
            if (SampleLoader.SL_Load(m_Samples[handle], sload, length))
                return -1;

            /* Unclick sample */
            if ((s.flags & SharpMikCommon.SF_LOOP) == SharpMikCommon.SF_LOOP)
            {
                if ((s.flags & SharpMikCommon.SF_BIDI) == SharpMikCommon.SF_BIDI)
                {
                    for (t = 0; t < 16; t++)
                    {
                        m_Samples[handle][loopend + t] = m_Samples[handle][(loopend - t) - 1];
                    }
                }
                else
                {
                    for (t = 0; t < 16; t++)
                    {
                        m_Samples[handle][loopend + t] = m_Samples[handle][t + loopstart];
                    }
                }
            }
            else
            {
                for (t = 0; t < 16; t++)
                {
                    m_Samples[handle][t + length] = 0;
                }
            }

            return (short)handle;
        }

        public void SampleUnload(short handle)
        {
            if (handle < m_Samples.Count)
            {
                m_Samples[handle] = null;
            }
        }

        public uint FreeSampleSpace(int value)
        {
            return (uint)m_VcMemory;
        }

        public uint RealSampleLength(int value, SAMPLE sample)
        {
            if (sample == null)
            {
                return 0;
            }

            return (uint)((sample.length * ((sample.flags & SharpMikCommon.SF_16BITS) == SharpMikCommon.SF_16BITS ? 2 : 1)) + 16);
        }


        public uint WriteBytes(sbyte[] buf, uint todo)
        {
            m_IsStereo = (m_VcMode & SharpMikCommon.DMODE_STEREO) == SharpMikCommon.DMODE_STEREO;

            if (m_VcSoftChannel == 0)
            {
                return SilenceBytes(buf, todo);
            }

            todo = bytes2samples(todo);
            WriteSamples(buf, todo);
            todo = samples2bytes(todo);

            // 			if (m_DspProcessor != null)
            // 			{
            // 				m_DspProcessor.PushData(buf, todo);
            // 			}

            return todo;
        }


        uint SilenceBytes(sbyte[] buf, uint todo)
        {
            todo = samples2bytes(bytes2samples(todo));

            sbyte toSet = 0;
            /* clear the buffer to zero (16 bits signed) or 0x80 (8 bits unsigned) */
            if ((m_VcMode & SharpMikCommon.DMODE_16BITS) == SharpMikCommon.DMODE_16BITS)
            {
                toSet = 0;
            }
            else
            {
                int value = 0x80;
                toSet = (sbyte)value;
            }

            for (int i = 0; i < todo; i++)
            {
                buf[i] = toSet;
            }

            return todo;
        }
    }
}

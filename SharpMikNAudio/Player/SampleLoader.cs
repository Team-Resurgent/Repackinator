using SharpMik.IO;

namespace SharpMik.Player
{
    class SampleLoader
    {
        static int sl_rlength;
        static short sl_old;
        static short[]? sl_buffer = null;
        static SAMPLOAD? musiclist = null;
        static SAMPLOAD? sndfxlist = null;

        /* size of the loader buffer in words */
        const uint SLBUFSIZE = 2048;

        /* IT-Compressed status structure */
        class ITPACK
        {
            public ushort bits;    /* current number of bits */
            public ushort bufbits; /* bits in buffer */
            public short last;    /* last output */
            public byte buf;     /* bit buffer */
        };

        public static bool SL_Init(SAMPLOAD s)
        {
            if (sl_buffer == null)
            {
                sl_buffer = new short[SLBUFSIZE];
            }

            sl_rlength = (int)s.length;
            if ((s.infmt & SharpMikCommon.SF_16BITS) == SharpMikCommon.SF_16BITS)
            {
                sl_rlength >>= 1;
            }

            sl_old = 0;

            return true;
        }

        public static void SL_Exit(SAMPLOAD s)
        {
            if (sl_rlength > 0)
            {
                s.reader.Seek(sl_rlength, SeekOrigin.Current);
            }

            if (sl_buffer != null)
            {
                sl_buffer = null;
            }
        }



        public static bool SL_Load(short[] buffer, SAMPLOAD smp, uint length)
        {
            return SL_LoadInternal(buffer, smp.infmt, smp.outfmt, smp.scalefactor, length, smp.reader, false);
        }


        static bool SL_LoadInternal(short[] buffer, uint infmt, uint outfmt, int scalefactor, uint length, ModuleReader reader, bool dither)
        {
            //SBYTE *bptr = (SBYTE*)buffer;
            //SWORD *wptr = (SWORD*)buffer;

            int stodo, t, u;

            //
            sbyte[] buf = new sbyte[buffer.Length * 2];

            int result, c_block = 0;    /* compression bytes until next block */
            ITPACK status = new ITPACK();
            status.buf = 0;
            status.last = 0;
            status.bufbits = 0;
            status.bits = 0;
            ushort incnt = 0;

            int index = 0;

            while (length != 0)
            {
                stodo = (int)((length < SLBUFSIZE) ? length : SLBUFSIZE);

                if ((infmt & SharpMikCommon.SF_ITPACKED) == SharpMikCommon.SF_ITPACKED)
                {
                    sl_rlength = 0;
                    if (c_block == 0)
                    {
                        status.bits = (ushort)((infmt & SharpMikCommon.SF_16BITS) == SharpMikCommon.SF_16BITS ? 17 : 9);

                        status.last = 0;
                        status.bufbits = 0;
                        incnt = reader.Read_Intel_ushort();// _mm_read_I_UWORD(reader);
                        c_block = (infmt & SharpMikCommon.SF_16BITS) == SharpMikCommon.SF_16BITS ? 0x4000 : 0x8000;
                        if ((infmt & SharpMikCommon.SF_DELTA) == SharpMikCommon.SF_DELTA)
                        {
                            sl_old = 0;
                        }
                    }

                    if ((infmt & SharpMikCommon.SF_16BITS) == SharpMikCommon.SF_16BITS)
                    {
                        result = read_itcompr16(status, reader, sl_buffer, (ushort)stodo, ref incnt);
                        if (result == 0)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        result = read_itcompr8(status, reader, sl_buffer, (ushort)stodo, ref incnt);
                        if (result == 0)
                        {
                            return true;
                        }
                    }

                    if (result != stodo)
                    {
                        throw new Exception("Invalid packed data");
                    }
                    c_block -= stodo;
                }
                else
                {
                    if ((infmt & SharpMikCommon.SF_16BITS) == SharpMikCommon.SF_16BITS)
                    {
                        if ((infmt & SharpMikCommon.SF_BIG_ENDIAN) == SharpMikCommon.SF_BIG_ENDIAN)
                            reader.read_Motorola_shorts(sl_buffer, stodo);
                        else
                            reader.read_Intel_shorts(sl_buffer, stodo);
                    }
                    else
                    {
                        sbyte[] bufff = new sbyte[stodo];

                        int read = stodo;
                        long left = reader.BaseStream.Length - reader.BaseStream.Position;
                        if (left < read)
                        {
                            read = (int)left;
                        }
                        reader.Read_bytes(bufff, read);

                        if (read != stodo)
                        {
                            sbyte[] tempBuf = new sbyte[stodo];
                            Buffer.BlockCopy(sl_buffer, 0, tempBuf, 0, stodo);

                            for (int i = read; i < stodo; i++)
                            {
                                bufff[i] = tempBuf[i];
                            }
                        }

                        for (int i = 0; i < stodo; i++)
                        {
                            sl_buffer[i] = (short)(bufff[i] << 8);
                        }
                    }
                    sl_rlength -= stodo;
                }

                if ((infmt & SharpMikCommon.SF_DELTA) == SharpMikCommon.SF_DELTA)
                {
                    for (t = 0; t < stodo; t++)
                    {
                        sl_buffer[t] += sl_old;
                        sl_old = sl_buffer[t];
                    }
                }

                if (((infmt ^ outfmt) & SharpMikCommon.SF_SIGNED) == SharpMikCommon.SF_SIGNED)
                {
                    for (t = 0; t < stodo; t++)
                    {
                        int s = sl_buffer[t];
                        s ^= 0x8000;
                        sl_buffer[t] = (short)s;
                    }
                }

                if (scalefactor != 0)
                {
                    int idx = 0;
                    int scaleval;

                    /* Sample Scaling... average values for better results. */
                    t = 0;
                    while (t < stodo && length != 0)
                    {
                        scaleval = 0;
                        for (u = scalefactor; u != 0 && t < stodo; u--, t++)
                        {
                            scaleval += sl_buffer[t];
                        }

                        sl_buffer[idx++] = (short)(scaleval / (scalefactor - u));
                        length--;
                    }
                    stodo = idx;
                }
                else
                {
                    length -= (uint)stodo;
                }

                if (dither)
                {
                    if ((infmt & SharpMikCommon.SF_STEREO) == SharpMikCommon.SF_STEREO && !((outfmt & SharpMikCommon.SF_STEREO) == SharpMikCommon.SF_STEREO))
                    {
                        /* dither stereo to mono, average together every two samples */
                        int avgval;
                        int idx = 0;

                        t = 0;
                        while (t < stodo && length != 0)
                        {
                            avgval = sl_buffer[t++];
                            avgval += sl_buffer[t++];
                            sl_buffer[idx++] = (short)(avgval >> 1);
                            length -= 2;
                        }
                        stodo = idx;
                    }
                }

                if ((outfmt & SharpMikCommon.SF_16BITS) == SharpMikCommon.SF_16BITS)
                {
                    for (t = 0; t < stodo; t++)
                    {
                        buffer[index++] = sl_buffer[t];
                        //buf[index++] = (sbyte)(sl_buffer[t] & 0xFF);
                        //buf[index++] = (sbyte)((sl_buffer[t] >> 8) & 0xFF);
                    }
                }
                else
                {
                    for (t = 0; t < stodo; t++)
                    {
                        buf[index++] = (sbyte)(sl_buffer[t] >> 8);
                    }
                }
            }

            if (!((outfmt & SharpMikCommon.SF_16BITS) == SharpMikCommon.SF_16BITS))
            {
                int j = 0;
                for (int i = 0; i < buffer.Length; i++)
                {
                    short value1 = buf[j++];
                    short value2 = buf[j++];
                    short put = (short)((ushort)value1 | (ushort)(value2 << 8));
                    buffer[i] = put;
                }
            }

            return false;
        }




        static int read_itcompr8(ITPACK status, ModuleReader reader, short[] buffer, ushort count, ref ushort incnt)
        {
            ushort x;
            int y, needbits, havebits, new_count = 0;
            ushort bits = status.bits;
            ushort bufbits = status.bufbits;
            sbyte last = (sbyte)status.last;
            byte buf = status.buf;

            int place = 0;

            while (place < count)
            {
                needbits = (new_count != 0 ? 3 : bits);
                x = 0;
                havebits = 0;

                while (needbits != 0)
                {
                    /* feed buffer */
                    if (bufbits == 0)
                    {
                        if (incnt-- > 0)
                        {
                            buf = reader.Read_byte();
                        }
                        else
                        {
                            buf = 0;
                        }
                        bufbits = 8;
                    }
                    /* get as many bits as necessary */
                    y = needbits < bufbits ? needbits : bufbits;
                    x |= (ushort)((buf & ((1 << y) - 1)) << havebits);
                    buf >>= y;
                    bufbits -= (ushort)y;
                    needbits -= y;
                    havebits += y;
                }

                if (new_count != 0)
                {
                    new_count = 0;
                    if (++x >= bits)
                        x++;
                    bits = (ushort)x;
                    continue;
                }
                if (bits < 7)
                {
                    if (x == (1 << (bits - 1)))
                    {
                        new_count = 1;
                        continue;
                    }
                }
                else if (bits < 9)
                {
                    y = (0xff >> (9 - bits)) - 4;
                    if ((x > y) && (x <= y + 8))
                    {
                        if ((x -= (ushort)y) >= bits)
                            x++;
                        bits = (ushort)x;
                        continue;
                    }
                }
                else if (bits < 10)
                {
                    if (x >= 0x100)
                    {
                        bits = (ushort)(x - 0x100 + 1);
                        continue;
                    }
                }
                else
                {
                    /* error in compressed data... */
                    throw new Exception("Invalid Data");
                }

                if (bits < 8) /* extend sign */
                {
                    x = (ushort)(((sbyte)(x << (8 - bits))) >> (8 - bits));
                }

                short val = (short)((last += (sbyte)x) << 8);
                buffer[place++] = (short)val;
            }

            status.bits = bits;
            status.bufbits = bufbits;
            status.last = last;
            status.buf = buf;

            return place;
        }

        static int read_itcompr16(ITPACK status, ModuleReader reader, short[] buffer, ushort count, ref ushort incnt)
        {
            int x, y, needbits, havebits, new_count = 0;
            ushort bits = status.bits;
            ushort bufbits = status.bufbits;
            short last = status.last;
            byte buf = status.buf;

            int place = 0;


            while (place < count)
            {
                needbits = new_count != 0 ? 4 : bits;
                x = havebits = 0;

                while (needbits != 0)
                {
                    /* feed buffer */
                    if (bufbits == 0)
                    {
                        if (incnt-- > 0)
                        {
                            // Turns out the normal mikmod implementation just sorta ignores EOF when reading samples...
                            // so in this situation I've changed the mikMod test application to fail in these situations
                            // and going to get sharpMik todo the same.
                            buf = reader.Read_byte();

                            if (reader.isEOF())
                            {
                                throw new Exception("EOF while reading the samples!");
                            }
                        }
                        else
                        {
                            buf = 0;
                        }
                        bufbits = 8;
                    }


                    /* get as many bits as necessary */
                    y = needbits < bufbits ? needbits : bufbits;
                    x |= (buf & ((1 << y) - 1)) << havebits;
                    buf >>= y;
                    bufbits = (ushort)(bufbits - (ushort)y);
                    needbits -= y;
                    havebits += y;
                }



                if (new_count != 0)
                {
                    new_count = 0;
                    if (++x >= bits)
                        x++;
                    bits = (ushort)x;
                    continue;
                }

                if (bits < 7)
                {
                    if (x == (1 << (bits - 1)))
                    {
                        new_count = 1;
                        continue;
                    }
                }
                else if (bits < 17)
                {
                    y = (0xffff >> (17 - bits)) - 8;

                    if ((x > y) && (x <= y + 16))
                    {
                        if ((x -= y) >= bits)
                            x++;

                        bits = (ushort)(x);
                        continue;
                    }
                }
                else if (bits < 18)
                {
                    if (x >= 0x10000)
                    {
                        bits = (ushort)(x - 0x10000 + 1);
                        continue;
                    }
                }
                else
                {
                    /* error in compressed data... */
                    throw new Exception("Invalid data");
                }

                if (bits < 16) /* extend sign */
                    x = ((short)(x << (16 - bits))) >> (16 - bits);

                buffer[place++] = (last += (short)x);
            }
            status.bits = bits;
            status.bufbits = bufbits;
            status.last = last;
            status.buf = buf;
            return place;
        }


        public static void SL_SampleSigned(SAMPLOAD s)
        {
            s.outfmt |= SharpMikCommon.SF_SIGNED;
            s.sample.flags = (ushort)(((ushort)(s.sample.flags & ~SharpMikCommon.SF_FORMATMASK)) | s.outfmt);
        }

        public static void SL_Sample8to16(SAMPLOAD s)
        {
            s.outfmt |= SharpMikCommon.SF_16BITS;
            s.sample.flags = (ushort)(((ushort)(s.sample.flags & ~SharpMikCommon.SF_FORMATMASK)) | s.outfmt);
        }

        public static bool SL_LoadSamples()
        {
            bool ok = true;

            //_mm_critical = 0;

            if ((musiclist == null) && (sndfxlist == null))
            {
                return false;
            }

            ok = DitherSamples(musiclist, (int)SharpMikCommon.MDTypes.MD_MUSIC) || DitherSamples(sndfxlist, (int)SharpMikCommon.MDTypes.MD_SNDFX);

            musiclist = null;
            sndfxlist = null;

            return ok;
        }



        static bool DitherSamples(SAMPLOAD samplist, int type)
        {
            SAMPLOAD s;

            if (samplist == null)
                return false;

#if DITHERSAMPLES // Not sure if we really ever do this on the software render.
			SAMPLOAD c2smp = null;
			uint maxsize, speed;

			if ((maxsize = MD_SampleSpace(type) * 1024))
			{
				while (SampleTotal(samplist, type) > maxsize)
				{
					/* First Pass - check for any 16 bit samples */
					s = samplist;
					while (s)
					{
						if (s->outfmt & SF_16BITS)
						{
							SL_Sample16to8(s);
							break;
						}
						s = s->next;
					}
					/* Second pass (if no 16bits found above) is to take the sample with
					   the highest speed and dither it by half. */
					if (!s)
					{
						s = samplist;
						speed = 0;
						while (s)
						{
							if ((s->sample->length) && (RealSpeed(s) > speed))
							{
								speed = RealSpeed(s);
								c2smp = s;
							}
							s = s->next;
						}
						if (c2smp)
							SL_HalveSample(c2smp, 2);
					}
				}
			}
#endif
            s = samplist;

            while (s != null)
            {
                /* sample has to be loaded ? -> increase number of samples, allocate
				   memory and load sample. */
                if (s.sample.length != 0)
                {
                    if (s.sample.seekpos != 0)
                    {
                        s.reader.Seek((int)s.sample.seekpos, SeekOrigin.Begin);
                    }

                    /* Call the sample load routine of the driver module. It has to
					   return a 'handle' (>=0) that identifies the sample. */
                    s.sample.handle = ModDriver.MD_SampleLoad(s, type);
                    s.sample.flags = (ushort)((ushort)(s.sample.flags & ~SharpMikCommon.SF_FORMATMASK) | s.outfmt);

                    if (s.sample.handle < 0)
                    {
                        FreeSampleList(samplist);
                        return false;
                    }
                }
                s = s.next;
            }

            return true;
        }


        static void FreeSampleList(SAMPLOAD s)
        {

        }


        public static SAMPLOAD SL_RegisterSample(SAMPLE s, int type, ModuleReader reader)
        {
            SAMPLOAD news;
            SAMPLOAD cruise = null;

            if (type == (int)SharpMikCommon.MDTypes.MD_MUSIC)
            {
                cruise = musiclist;
            }
            else if (type == (int)SharpMikCommon.MDTypes.MD_SNDFX)
            {
                cruise = sndfxlist;
            }
            else
            {
                return null;
            }

            /* Allocate and add structure to the END of the list */
            news = new SAMPLOAD();

            if (cruise != null)
            {
                while (cruise.next != null)
                {
                    cruise = cruise.next;
                }
                cruise.next = news;
            }
            else
            {
                if (type == (int)SharpMikCommon.MDTypes.MD_MUSIC)
                {
                    musiclist = news;
                }
                else if (type == (int)SharpMikCommon.MDTypes.MD_SNDFX)
                {
                    sndfxlist = news;
                }
            }

            news.infmt = (uint)(s.flags & SharpMikCommon.SF_FORMATMASK);
            news.outfmt = news.infmt;
            news.reader = reader;
            news.sample = s;
            news.length = s.length;
            news.loopstart = s.loopstart;
            news.loopend = s.loopend;

            return news;
        }
    }
}

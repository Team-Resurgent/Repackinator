using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpMik
{
	/* 
	Sparse description of the internal module format
	------------------------------------------------

	A UNITRK stream is an array of bytes representing a single track of a pattern.
	It's made up of 'repeat/length' bytes, opcodes and operands (sort of a assembly
	language):

	rrrlllll
	[REP/LEN][OPCODE][OPERAND][OPCODE][OPERAND] [REP/LEN][OPCODE][OPERAND]..
	^                                         ^ ^
	|-------ROWS 0 - 0+REP of a track---------| |-------ROWS xx - xx+REP of a track...

	  The rep/len byte contains the number of bytes in the current row, _including_
	the length byte itself (So the LENGTH byte of row 0 in the previous example
	would have a value of 5). This makes it easy to search through a stream for a
	particular row. A track is concluded by a 0-value length byte.

	  The upper 3 bits of the rep/len byte contain the number of times -1 this row
	is repeated for this track. (so a value of 7 means this row is repeated 8 times)

	  Opcodes can range from 1 to 255 but currently only opcodes 1 to 62 are being
	used. Each opcode can have a different number of operands. You can find the
	number of operands to a particular opcode by using the opcode as an index into
	the 'unioperands' table.

	*/
	public class munitrk
	{
		#region const variables

		const int BUFPAGE   = 128;

		public static ushort[] unioperands ={
			0, /* not used */
			1, /* UNI_NOTE */
			1, /* UNI_INSTRUMENT */
			1, /* UNI_PTEFFECT0 */
			1, /* UNI_PTEFFECT1 */
			1, /* UNI_PTEFFECT2 */
			1, /* UNI_PTEFFECT3 */
			1, /* UNI_PTEFFECT4 */
			1, /* UNI_PTEFFECT5 */
			1, /* UNI_PTEFFECT6 */
			1, /* UNI_PTEFFECT7 */
			1, /* UNI_PTEFFECT8 */
			1, /* UNI_PTEFFECT9 */
			1, /* UNI_PTEFFECTA */
			1, /* UNI_PTEFFECTB */
			1, /* UNI_PTEFFECTC */
			1, /* UNI_PTEFFECTD */
			1, /* UNI_PTEFFECTE */
			1, /* UNI_PTEFFECTF */
			1, /* UNI_S3MEFFECTA */
			1, /* UNI_S3MEFFECTD */
			1, /* UNI_S3MEFFECTE */
			1, /* UNI_S3MEFFECTF */
			1, /* UNI_S3MEFFECTI */
			1, /* UNI_S3MEFFECTQ */
			1, /* UNI_S3MEFFECTR */
			1, /* UNI_S3MEFFECTT */
			1, /* UNI_S3MEFFECTU */
			0, /* UNI_KEYOFF */
			1, /* UNI_KEYFADE */
			2, /* UNI_VOLEFFECTS */
			1, /* UNI_XMEFFECT4 */
			1, /* UNI_XMEFFECT6 */
			1, /* UNI_XMEFFECTA */
			1, /* UNI_XMEFFECTE1 */
			1, /* UNI_XMEFFECTE2 */
			1, /* UNI_XMEFFECTEA */
			1, /* UNI_XMEFFECTEB */
			1, /* UNI_XMEFFECTG */
			1, /* UNI_XMEFFECTH */
			1, /* UNI_XMEFFECTL */
			1, /* UNI_XMEFFECTP */
			1, /* UNI_XMEFFECTX1 */
			1, /* UNI_XMEFFECTX2 */
			1, /* UNI_ITEFFECTG */
			1, /* UNI_ITEFFECTH */
			1, /* UNI_ITEFFECTI */
			1, /* UNI_ITEFFECTM */
			1, /* UNI_ITEFFECTN */
			1, /* UNI_ITEFFECTP */
			1, /* UNI_ITEFFECTT */
			1, /* UNI_ITEFFECTU */
			1, /* UNI_ITEFFECTW */
			1, /* UNI_ITEFFECTY */
			2, /* UNI_ITEFFECTZ */
			1, /* UNI_ITEFFECTS0 */
			2, /* UNI_ULTEFFECT9 */
			2, /* UNI_MEDSPEED */
			0, /* UNI_MEDEFFECTF1 */
			0, /* UNI_MEDEFFECTF2 */
			0, /* UNI_MEDEFFECTF3 */
			2, /* UNI_OKTARP */
		};
		#endregion


		#region Read Functions
		byte[] m_RowData; /* startadress of a row */
		int m_RowEnd;   /* endaddress of a row (exclusive) */
		int m_CurrentRowPosition;    /* current unimod(tm) programcounter */

		byte lastbyte;  /* for UniSkipOpcode() */

		public void UniSetRow(byte[] t, int place)
		{
			m_RowData = t;
			m_CurrentRowPosition    = place;
			m_RowEnd = (m_CurrentRowPosition + (m_RowData[m_CurrentRowPosition++] & 0x1f));
		}

		public byte UniGetByte()
		{
			if (m_CurrentRowPosition < m_RowEnd)
			{
				lastbyte = m_RowData[m_CurrentRowPosition++];
			}
			else
			{
				lastbyte = 0;
			}

			return lastbyte;// = (byte)((rowpc<rowend)? (rowstart[rowpc++]):0);
		}

		public ushort UniGetWord()
		{
			return (ushort)(((ushort)UniGetByte()<<8)|UniGetByte());
		}

		public void UniSkipOpcode()
		{
			if (lastbyte < unioperands.Length) 
			{
				ushort t = unioperands[lastbyte];

				while (t-- != 0)
				{
					UniGetByte();
				}
			}
		}

		/* Finds the address of row number 'row' in the UniMod(tm) stream 't' returns
		   NULL if the row can't be found. */
		public int UniFindRow(byte[] t, ushort row)
		{
			byte c,l;

			int place = 0;

			if (t != null)
			{
				while (true)
				{
					c = t[place];             /* get rep/len byte */

					if (c == 0)
					{
						return -1; /* zero ? . end of track.. */
					}

					l = (byte)((c >> 5) + 1);       /* extract repeat value */
					
					if (l > row) 
						break;    /* reached wanted row? . return pointer */
					row -= l;           /* haven't reached row yet.. update row */
					place += c & 0x1f;
				}
			}
			
			return place;
		}
		#endregion

		#region Writing routines

		byte[] m_WriteBuffer;			/* pointer to the temporary unitrk buffer */
		ushort m_WriteBufferSize;		/* buffer size */

		ushort m_WriteBufferPosition;   /* buffer cursor */
		ushort m_CurrentRowIndex;		/* current row index */
		ushort m_LastRowIndex;			/* previous row index */

		public void UniReset()
		{
			m_CurrentRowIndex     = 0;   /* reset index to rep/len byte */
			m_WriteBufferPosition     = 1;   /* first opcode will be written to index 1 */
			m_LastRowIndex     = 0;   /* no previous row yet */
			m_WriteBuffer[0] = 0;   /* clear rep/len byte */
		}

		public void UniInit()
		{
			m_WriteBufferSize = BUFPAGE;
			m_WriteBuffer = new byte[m_WriteBufferSize];
		}

		public bool UniExpand(int wanted)
		{
			if ((m_WriteBufferPosition + wanted) >= m_WriteBufferSize)
			{
				m_WriteBufferSize += BUFPAGE;
				Array.Resize(ref m_WriteBuffer, m_WriteBufferSize);
			}
			return true;
		}

		public void UniWriteByte(byte data)
		{
			if (UniExpand(1))
			{
				/* write byte to current position and update */
				m_WriteBuffer[m_WriteBufferPosition++] = data;
			}
		}

		public void UniWriteWord(ushort data)
		{
			if (UniExpand(2))
			{
				m_WriteBuffer[m_WriteBufferPosition++] = (byte)(data >> 8);
				m_WriteBuffer[m_WriteBufferPosition++] = (byte)(data & 0xff);
			}
		}

		public bool MyCmp(byte[] data, int a, int b, int l)
		{
			ushort t;

			for (t = 0; t < l; t++)
			{
				if (data[a + t] != data[b + t])
				{
					return false;
				}
			}
			return true;
		}

		/* Closes the current row of a unitrk stream (updates the rep/len byte) and sets
		   pointers to start a new row. */
		public void UniNewline()
		{
			ushort n,l,len;

			n = (ushort)((m_WriteBuffer[m_LastRowIndex]>>5)+1);     /* repeat of previous row */
			l = (ushort)(m_WriteBuffer[m_LastRowIndex]&0x1f);     /* length of previous row */

			len = (ushort)(m_WriteBufferPosition-m_CurrentRowIndex);            /* length of current row */

			/* Now, check if the previous and the current row are identical.. when they
			   are, just increase the repeat field of the previous row */
			//  MyCmp(&unibuf[lastp+1],&unibuf[unitt+1],len-1)
			if(n<8 && len==l && MyCmp(m_WriteBuffer, m_LastRowIndex+1,m_CurrentRowIndex+1,len-1)) 
			{
				m_WriteBuffer[m_LastRowIndex]+=0x20;
				m_WriteBufferPosition = (ushort)(m_CurrentRowIndex+1);
			} 
			else 
			{
				if (UniExpand(m_CurrentRowIndex-m_WriteBufferPosition)) 
				{
					/* current and previous row aren't equal... update the pointers */
					m_WriteBuffer[m_CurrentRowIndex] = (byte)len;
					m_LastRowIndex = m_CurrentRowIndex;
					m_CurrentRowIndex = m_WriteBufferPosition++;
				}
			}
		}


		public byte[] UniDup()
		{
			byte[] d;

			if (!UniExpand(m_WriteBufferPosition - m_CurrentRowIndex))
			{
				return null;
			}

			m_WriteBuffer[m_CurrentRowIndex] = 0;		

			d = new byte[m_WriteBufferPosition];

			Array.Copy(m_WriteBuffer, d, m_WriteBufferPosition);

			return d;
		}


		public void UniCleanup()
		{
			m_WriteBuffer = null;
			m_WriteBufferSize = 0;
		}
		#endregion
	}
}

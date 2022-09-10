using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repackinator.Shared
{
    public class ProgressStream : Stream
    {
        private int m_currentPart;
        private Stream[] m_outputParts;

        private long m_bytesProcessed;
                
        private long m_isoLength;
        private bool m_removeVideoPartition;
        private Action<float> m_progress;

        public ProgressStream(Stream outputPart1, Stream outputPart2, long isoLength, Action<float> progress)
        {
            m_currentPart = 0;
            m_outputParts = new Stream[2] { outputPart1, outputPart2 };

            m_isoLength = isoLength;
            m_progress = progress;
        }

        public override void Flush()
        {
            m_outputParts[m_currentPart].Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            const long redumpSize = 7825162240;
            const long videoSize = 387 * 1024 * 1024;

            long skipSize = m_isoLength == redumpSize ? videoSize : 0;           

            if (m_bytesProcessed < skipSize)
            {
                var remainder = (m_bytesProcessed + count) - skipSize;
                if (remainder > 0)
                {
                    offset = (count - (int)remainder) + offset;
                    m_bytesProcessed += count;
                    count = (int)remainder;                    
                }       
                else
                {
                    m_bytesProcessed += count;
                    count = 0;
                }                
            }

            if (count == 0)
            {
                m_progress(m_bytesProcessed / (float)m_isoLength);
                return;
            }

            long sectorSplitPosition = (((m_isoLength - skipSize) / 4096) * 2048) + skipSize;

            if (m_bytesProcessed < sectorSplitPosition)
            {
                var remainder = (m_bytesProcessed + count) - sectorSplitPosition;
                if (remainder > 0)
                {                    
                    m_bytesProcessed += count;
                    m_outputParts[0].Write(buffer, offset, count - (int)remainder);

                    offset = (count - (int)remainder) + offset;
                    count = (int)remainder;
                    m_outputParts[1].Write(buffer, offset, count);
                }
                else
                {
                    m_bytesProcessed += count;
                    m_outputParts[0].Write(buffer, offset, count);
                }
                m_progress(m_bytesProcessed / (float)m_isoLength);
                return;
            }

            m_outputParts[1].Write(buffer, offset, count);
            m_progress(m_bytesProcessed / (float)m_isoLength);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_outputParts[m_currentPart].Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            m_outputParts[m_currentPart].SetLength(value);
        }

        public override bool CanRead => m_outputParts[m_currentPart].CanRead;
        public override bool CanSeek => m_outputParts[m_currentPart].CanSeek;
        public override bool CanWrite => false;
        public override long Length => m_outputParts[m_currentPart].Length;
        public override long Position
        {
            get { return m_outputParts[m_currentPart].Position; }
            set { m_outputParts[m_currentPart].Position = value; }
        }
    }
}

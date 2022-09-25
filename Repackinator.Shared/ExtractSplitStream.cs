using System.Threading;

namespace Repackinator.Shared
{
    public class ExtractSplitStream : Stream
    {
        private int m_currentPart;
        private Stream[] m_outputParts;
        private long m_bytesProcessed;
        private long m_isoLength;
        private Action<float> m_progress;
        private CancellationToken m_cancellationToken;

        public ExtractSplitStream(Stream outputPart1, Stream outputPart2, long isoLength, Action<float> progress, CancellationToken cancellationToken)
        {
            m_currentPart = 0;
            m_outputParts = new Stream[] { outputPart1, outputPart2 };
            m_cancellationToken = cancellationToken;
            m_isoLength = isoLength;
            m_progress = progress;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (m_cancellationToken.IsCancellationRequested)
            {
                throw new ExtractAbortException();
            }

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
                    m_currentPart = 1;
                    m_outputParts[m_currentPart].Write(buffer, offset, count);
                }
                else
                {
                    m_bytesProcessed += count;
                    m_outputParts[m_currentPart].Write(buffer, offset, count);
                }
                m_progress(m_bytesProcessed / (float)m_isoLength);
                return;
            }

            m_bytesProcessed += count;
            m_outputParts[m_currentPart].Write(buffer, offset, count);
            m_progress(m_bytesProcessed / (float)m_isoLength);
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        public override void Flush() => throw new NotImplementedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();

        public override bool CanRead => throw new NotImplementedException();
        public override bool CanSeek => throw new NotImplementedException();
        public override bool CanWrite => throw new NotImplementedException();
        public override long Length => throw new NotImplementedException();

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
    }
}

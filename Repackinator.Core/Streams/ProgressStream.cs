using Repackinator.Core.Exceptions;

namespace Repackinator.Core.Streams
{
    public class ProgressStream : Stream
    {
        private Stream m_stream;
        private long m_length;
        private Action<float> m_progress;
        private CancellationToken m_cancellationToken;

        public ProgressStream(Stream stream, long length, Action<float> progress, CancellationToken cancellationToken)
        {
            m_stream = stream;
            m_length = length;
            m_progress = progress;
            m_cancellationToken = cancellationToken;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var result = m_stream.Read(buffer, offset, count);
            m_progress(m_stream.Position / (float)m_length);
            return result;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (m_cancellationToken.IsCancellationRequested)
            {
                throw new ExtractAbortException();
            }

            m_stream.Write(buffer, offset, count);
            m_progress(m_stream.Position / (float)m_length);
        }

        public override void Flush()
        {
            m_stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_stream.Seek(offset, origin);
        }
        public override void SetLength(long value)
        {
            m_stream.SetLength(Length);
        }

        public override bool CanRead => m_stream.CanRead;

        public override bool CanWrite => m_stream.CanWrite;

        public override bool CanSeek => m_stream.CanSeek;

        public override long Length => m_stream.Length;

        public override long Position
        {
            get { return m_stream.Position; }
            set { m_stream.Position = value; }
        }
    }
}
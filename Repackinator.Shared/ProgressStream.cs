using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repackinator.Shared
{
    public class ProgressStream : Stream
    {
        private Stream m_output;
        private long m_length = 0;
        private Action<float> m_progress;

        public ProgressStream(Stream output, long length, Action<float> progress)
        {
            m_output = output;
            m_length = length;
            m_progress = progress;
        }

        public override void Flush()
        {
            m_output.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var result = m_output.Read(buffer, offset, count);
            m_progress(m_output.Position / (float)m_length);
            return result;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            m_output.Write(buffer, offset, count);
            m_progress(m_output.Position / (float)m_length);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_output.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            m_output.SetLength(value);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => m_length;
        public override long Position
        {
            get { return m_output.Position; }
            set { m_output.Position = value; }
        }
    }
}

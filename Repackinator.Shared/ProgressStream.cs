﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repackinator.Shared
{
    public class ProgressStream : Stream
    {
        private Stream m_output;
        private long m_length;
        private bool m_removeVideoPartition;
        private Action<float> m_progress;

        public ProgressStream(Stream output, long length, bool removeVideoPartition, Action<float> progress)
        {
            m_output = output;
            m_length = length;
            m_removeVideoPartition = removeVideoPartition; 
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
            const long dvdSize = 7825162240;
            long videoSize = 387 * 1024 * 1024;

            long skipSize = (m_removeVideoPartition == true && m_length == dvdSize) ? videoSize : 0;
            long realLength = m_length - skipSize;

            //long sectorSplit = ((fileLength - skipSize) / 4096) * 2048;

            m_output.Write(buffer, offset, count);
            m_progress(m_output.Position / (float)realLength);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_output.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            m_output.SetLength(value);
        }

        public override bool CanRead => m_output.CanRead;
        public override bool CanSeek => m_output.CanSeek;
        public override bool CanWrite => m_output.CanWrite;
        public override long Length => m_output.Length;
        public override long Position
        {
            get { return m_output.Position; }
            set { m_output.Position = value; }
        }
    }
}
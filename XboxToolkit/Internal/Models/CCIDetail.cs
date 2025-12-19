using System.IO;

namespace XboxToolkit.Internal.Models
{
    internal struct CCIDetail
    {
        public Stream Stream;
        public CCIIndex[] IndexInfo;
        public long StartSector;
        public long EndSector;
    }
}

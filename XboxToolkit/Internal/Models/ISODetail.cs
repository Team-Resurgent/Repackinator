using System.IO;

namespace XboxToolkit.Internal.Models
{
    internal struct ISODetail
    {
        public Stream Stream;
        public long StartSector;
        public long EndSector;
    }
}

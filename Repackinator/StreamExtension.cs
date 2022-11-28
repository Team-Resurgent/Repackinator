namespace Repackinator
{
    public static class StreamExtension
    {
        public static ushort ReadUInt16(this Stream stream)
        {
            byte[] buffer = new byte[sizeof(ushort)];
            if (stream.Read(buffer, 0, sizeof(ushort)) != sizeof(ushort))
            {
                throw new Exception();
            }
            return BitConverter.ToUInt16(buffer, 0);
        }

        public static uint ReadUInt32(this Stream stream)
        {
            byte[] buffer = new byte[sizeof(uint)];
            if (stream.Read(buffer, 0, sizeof(uint)) != sizeof(uint))
            {
                throw new Exception();
            }
            return BitConverter.ToUInt32(buffer, 0);
        }

        public static ulong ReadUInt64(this Stream stream)
        {
            byte[] buffer = new byte[sizeof(ulong)];
            if (stream.Read(buffer, 0, sizeof(ulong)) != sizeof(ulong))
            {
                throw new Exception();
            }
            return BitConverter.ToUInt64(buffer, 0);
        }
    }
}

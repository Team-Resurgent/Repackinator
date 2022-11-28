namespace Resurgent.UtilityBelt.Library.Utilities.ImageInput
{
    public interface IImageInput : IDisposable
    {
        long TotalSectors { get; }
        byte ReadByte(long position);
        byte[] ReadBytes(long position, uint length);
        byte[] ReadSectors(long startSector, long count);
        ushort ReadUint16(long position);
        uint ReadUint32(long position);
    }
}
namespace SharpMik.DSP
{
    public abstract class Idsp
    {
        public abstract void PushData(sbyte[] data, uint count);
    }
}

using System;

namespace XboxToolkit
{
    [Flags]
    public enum ProcessingOptions
    {
        OneToOneCopy = 0,
        RemoveVideoPartition = 1,
        ScrubSectors = 2,
        TrimSectors = 4, 
        All = RemoveVideoPartition | ScrubSectors | TrimSectors
    }
}

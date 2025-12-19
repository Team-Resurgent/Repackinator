using System;

namespace XboxToolkit
{
    [Flags]
    public enum XexRegion
    {
        Unknown = 0,
        USA = 1,
        Japan = 2,
        Europe = 4,
        Global = 7
    }
}

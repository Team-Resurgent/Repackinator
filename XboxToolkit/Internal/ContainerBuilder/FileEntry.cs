namespace XboxToolkit.Internal.ContainerBuilder
{
    internal class FileEntry
    {
        public string RelativePath { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public uint Size { get; set; }
        public uint Sector { get; set; }
    }
}


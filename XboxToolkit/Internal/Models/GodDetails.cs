namespace XboxToolkit.Internal.Models
{
    internal struct GODDetails
    {
        public string DataPath;
        public uint DataFileCount;
        public uint BaseAddress;
        public uint StartingBlock;
        public uint SectorCount;
        public bool IsEnhancedGDF;
    }
}

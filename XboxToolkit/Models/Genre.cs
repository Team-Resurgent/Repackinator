namespace XboxToolkit.Models
{
    public struct Genre
    {
        public uint Id { get; set; }
        public string Name { get; set; }

        public Genre(uint id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}

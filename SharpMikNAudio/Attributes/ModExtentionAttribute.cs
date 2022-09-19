namespace SharpMik.Attributes
{
    public class ModFileExtentionsAttribute : Attribute
    {
        public string[] FileExtentions { get; }


        public ModFileExtentionsAttribute(params string[] extentions)
        {
            FileExtentions = extentions;
        }
    }
}

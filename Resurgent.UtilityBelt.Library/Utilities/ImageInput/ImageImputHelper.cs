namespace Resurgent.UtilityBelt.Library.Utilities.ImageInput
{
    public static class ImageImputHelper
    {
        public static IImageInput GetImageInput(string[] filenames)
        {
            var extension = Path.GetExtension(filenames[0]);
            if (extension.Equals(".iso", StringComparison.CurrentCultureIgnoreCase))
            {
                return new XisoInput(filenames);
            }
            if (extension.Equals(".cso", StringComparison.CurrentCultureIgnoreCase))
            {
                return new CsoInput(filenames);
            }
            if (extension.Equals(".cci", StringComparison.CurrentCultureIgnoreCase))
            {
                return new CciInput(filenames);
            }
            throw new NotImplementedException($"Unknown ImageInput format '{extension}'.");
        }
    }
}

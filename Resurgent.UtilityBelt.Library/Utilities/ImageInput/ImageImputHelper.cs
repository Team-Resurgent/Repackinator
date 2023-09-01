namespace Resurgent.UtilityBelt.Library.Utilities.ImageInput
{
    public static class ImageImputHelper
    {
        private static string[] GetSlicesFromFile(string filename)
        {
            var slices = new List<string>();
            var extension = Path.GetExtension(filename);
            var fileWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            var subExtension = Path.GetExtension(fileWithoutExtension);
            if (subExtension.Equals(".1") || subExtension.Equals(".2"))
            {
                var fileWithoutSubExtension = Path.GetFileNameWithoutExtension(fileWithoutExtension);
                var directory = Path.GetDirectoryName(filename);
                if (directory != null)
                {
                    for (var i = 1; i <= 2; i++)
                    {
                        var fileToAdd = Path.Combine(directory, $"{fileWithoutSubExtension}.{i}{extension}");
                        if (File.Exists(fileToAdd))
                        {
                            slices.Add(fileToAdd);
                        }
                    }
                }
            }
            else
            {
                slices.Add(filename);
            }
            slices.Sort();
            return slices.ToArray();
        }

        public static IImageInput GetImageInput(string filename)
        {
            return GetImageInput(GetSlicesFromFile(filename));
        }

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

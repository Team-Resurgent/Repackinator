using System;

namespace XboxToolkit.Models
{
    public struct MarketplaceMetaData
    {
        public string TitleId;
        public string Title;
        public string Developer;
        public string Publisher;
        public GenreType Genre;
        public string Description;
        public byte[] TitleImage;
        public byte[] BackgroundImage;
        public byte[] BannerImage;
        public byte[] BoxArtImage;

        public MarketplaceMetaData()
        {
            TitleId = string.Empty;
            Description = string.Empty;
            Developer = string.Empty;
            Genre = GenreType.Unknown;
            Publisher = string.Empty;
            Title = string.Empty;
            TitleImage = Array.Empty<byte>();
            BackgroundImage = Array.Empty<byte>();
            BannerImage = Array.Empty<byte>();
            BoxArtImage = Array.Empty<byte>();
        }
    }
}

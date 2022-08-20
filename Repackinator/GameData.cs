using Newtonsoft.Json;

namespace Repackinator
{
    public class GameData
    {
        [JsonProperty("Title ID")]
        public string? TitleID { get; set; }

        [JsonProperty("Title Name")]
        public string? TitleName { get; set; }

        [JsonProperty("Version")]
        public string? Version { get; set; }

        [JsonProperty("Region")]
        public string? Region { get; set; }

        [JsonProperty("Letter")]
        public string? Letter { get; set; }

        [JsonProperty("XBE Title & Folder Name")]
        public string? XBETitleAndFolderName { get; set; }

        [JsonProperty("ISO Name")]
        public string? ISOName { get; set; }

        [JsonProperty("Process")]
        public string? Process { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace Repackinator.Shared
{
    public struct GameData
    {
        [JsonPropertyName("Title ID")]
        public string TitleID { get; set; }

        [JsonPropertyName("Title Name")]
        public string TitleName { get; set; }

        [JsonPropertyName("Version")]
        public string Version { get; set; }

        [JsonPropertyName("Region")]
        public string Region { get; set; }

        [JsonPropertyName("Letter")]
        public string Letter { get; set; }

        [JsonPropertyName("XBE Title")]
        public string XBETitle { get; set; }

        [JsonPropertyName("XBE Title Alt")]
        public string XBETitleAlt { get; set; }

        [JsonPropertyName("Folder Name")]
        public string FolderName { get; set; }

        [JsonPropertyName("Folder Name Alt")]
        public string FolderNameAlt { get; set; }

        [JsonPropertyName("ISO Name")]
        public string ISOName { get; set; }

        [JsonPropertyName("ISO Name Alt")]
        public string ISONameAlt { get; set; }

        [JsonPropertyName("ISO Checksum")]
        public string ISOChecksum { get; set; }

        [JsonPropertyName("Process")]
        public string Process { get; set; }
    }
}

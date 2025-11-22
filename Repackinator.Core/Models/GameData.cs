using Repackinator.Core.Helpers;
using System.Text.Json.Serialization;

namespace Repackinator.Core.Models
{
    public struct GameData
    {
        [JsonIgnore]
        public bool Selected { get; set; }

        [JsonIgnore]
        public int Index { get; set; }

        [JsonPropertyName("List")]
        public string Section { get; set; }

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

        [JsonPropertyName("Folder Name")]
        public string FolderName { get; set; }

        [JsonPropertyName("ISO Name")]
        public string ISOName { get; set; }

        [JsonPropertyName("ISO Checksum")]
        public string ISOChecksum { get; set; }

        [JsonPropertyName("Process")]
        public string Process { get; set; }

        [JsonPropertyName("Category")]
        public string Category { get; set; }

        [JsonPropertyName("Link")]
        public string Link { get; set; }

        [JsonPropertyName("Info")]
        public string Info { get; set; }

        [JsonIgnore]
        public bool IsValidXBETitle
        {
            get
            {
                if (XBETitle.Length > 40 && Utility.ValidateFatX(XBETitle))
                {
                    return false;
                }
                return true;
            }
        }

        [JsonIgnore]
        public bool IsValidFolderName
        {
            get
            {
                if (FolderName.Length > 42 && Utility.ValidateFatX(FolderName))
                {
                    return false;
                }
                return true;
            }
        }

        [JsonIgnore]
        public bool IsValidISOName
        {
            get
            {
                if (ISOName.Length > 36 && Utility.ValidateFatX(ISOName))
                {
                    return false;
                }
                return true;
            }
        }

        [JsonIgnore]
        public bool IsValid => IsValidXBETitle && IsValidFolderName && IsValidISOName;
    }
}

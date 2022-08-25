using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Repackinator.Shared
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

        [JsonProperty("XBE Title & Folder Name Alt")]
        public string? XBETitleAndFolderNameAlt { get; set; }

        [JsonProperty("ISO Name")]
        public string? ISOName { get; set; }

        [JsonProperty("ISO Name Alt")]
        public string? ISONameAlt { get; set; }

        [JsonProperty("Process")]
        public string? Process { get; set; }

        public static GameData[]? LoadGameData(string path)
        {
            var gameDataJson = File.ReadAllText(path);
            var result = JsonConvert.DeserializeObject<GameData[]>(gameDataJson);           
            return result;
        }

        public static GameData[]? LoadGameData()
        {
            var applicationPath = Utility.GetApplicationPath();
            if (applicationPath == null)
            {
                return null;
            }

            var repackListPath = Path.Combine(applicationPath, "RepackList.json");
            if (!File.Exists(repackListPath))
            {
                return null;
            }

            return LoadGameData(repackListPath);
        }
    }
}

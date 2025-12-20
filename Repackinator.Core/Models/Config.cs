using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Repackinator.Core.Helpers;

namespace Repackinator.Core.Models
{
    public enum GameDataFilterType
    {
        Scrub,
        Process,
        [Description("Title ID")]
        TitleID,
        Region,
        Version,
        [Description("Title Name")]
        TitleName,
        Letter,
        [Description("XBE Title")]
        XBETitle,
        [Description("Folder Name")]
        FolderName,
        [Description("ISO Name")]
        ISOName,
        [Description("ISO Checksum")]
        ISOChecksum
    }

    public enum GroupingOptionType
    {
        None,
        Region,
        Letter,
        [Description("Region Letter")]
        RegionLetter,
        [Description("Letter Region")]
        LetterRegion
    }

    public enum CompressOptionType
    {
        None,
        CCI
    }

    public enum ScrubOptionType
    {
        None,
        Scrub,
        TrimScrub
    }

    public class Config
    {
        public string Section { get; set; }

        public GameDataFilterType FilterType { get; set; }

        [JsonIgnore]
        public int LeechType { get; set; }

        public string CompareFirst { get; set; }

        public string CompareSecond { get; set; }

        public GroupingOptionType GroupingOption { get; set; }

        public bool Uppercase { get; set; }

        public CompressOptionType CompressOption { get; set; }

        public ScrubOptionType ScrubOption { get; set; }

        public bool RecurseInput { get; set; }

        public bool NoSplit { get; set; }

        public string InputPath { get; set; }

        public string OutputPath { get; set; }

        public string UnpackPath { get; set; }

        public Config()
        {
            Section = "Main";
            FilterType = GameDataFilterType.TitleName;
            LeechType = 0;
            CompareFirst = string.Empty;
            CompareSecond = string.Empty;
            GroupingOption = GroupingOptionType.None;
            Uppercase = false;
            CompressOption = CompressOptionType.None;
            ScrubOption = ScrubOptionType.Scrub;
            RecurseInput = false;
            NoSplit = false;
            InputPath = string.Empty;
            OutputPath = string.Empty;
            UnpackPath = string.Empty;
        }

        public static Config LoadConfig(string path)
        {
            var configJson = File.ReadAllText(path);
            var result = JsonSerializer.Deserialize<Config>(configJson);
            return result ?? new();
        }

        public static Config LoadConfig()
        {
            var applicationPath = Utility.GetApplicationPath();
            if (applicationPath == null)
            {
                return new Config();
            }

            var configPath = Path.Combine(applicationPath, "config.json");
            if (!File.Exists(configPath))
            {
                return new Config();
            }

            return LoadConfig(configPath);
        }

        public static void SaveConfig(string path, Config? config)
        {
            if (config == null)
            {
                return;
            }

            var result = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, result);
        }

        public static void SaveConfig(Config? config)
        {
            var applicationPath = Utility.GetApplicationPath();
            if (applicationPath == null)
            {
                return;
            }

            var configPath = Path.Combine(applicationPath, "config.json");
            SaveConfig(configPath, config);
        }

    }
}

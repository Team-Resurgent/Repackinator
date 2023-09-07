using System.Text.Json;
using System.Text.Json.Serialization;
using Repackinator.Helpers;

namespace Repackinator.Models
{
    public enum GroupingEnum
    {
        None,
        Region,
        Letter,
        RegionLetter,
        LetterRegion
    }

    public enum CompressEnum
    {
        None,
        CCI,
        CSO
    }

    public class Config
    {
        public string InputPath { get; set; }

        public string OutputPath { get; set; }

        public GroupingEnum Grouping { get; set; }

        public bool RecurseInput { get; set; }

        public bool UpperCase { get; set; }

        public CompressEnum CompressType { get; set; }

        public bool NoSplit { get; set; }

        public bool TrimmedScrub { get; set; }

        [JsonIgnore]
        public int LeechType { get; set; }

        public int SearchField { get; set; }

        public string CompareFirst { get; set; }

        public string CompareSecond { get; set; }

        public Config()
        {
            InputPath = string.Empty;
            OutputPath = string.Empty;
            Grouping = GroupingEnum.None;
            RecurseInput = false;
            UpperCase = false;
            CompressType = CompressEnum.None;
            NoSplit = false;
            LeechType = 0;
            TrimmedScrub = false;
            SearchField = 0;
            CompareFirst = string.Empty;
            CompareSecond = string.Empty;
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

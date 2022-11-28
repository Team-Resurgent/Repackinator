using System.Text.Json;

namespace Repackinator.Shared
{
    public enum GroupingEnum
    {
        None,
        Region,
        Letter,
        RegionLetter,
        LetterRegion
    }

    public struct Config
    {
        public string InputPath { get; set; }

        public string OutputPath { get; set; }

        public GroupingEnum Grouping { get; set; }

        public bool RecurseInput { get; set; }

        public bool UpperCase { get; set; }

        public bool Compress { get; set; }

        public bool TrimmedScrub { get; set; }

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
            Compress = false;
            TrimmedScrub = false;
            SearchField = 0;
            CompareFirst = string.Empty;
            CompareSecond = string.Empty;
        }

        public static Config LoadConfig(string path)
        {
            var configJson = File.ReadAllText(path);
            var result = JsonSerializer.Deserialize<Config>(configJson);
            return result;
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

using Newtonsoft.Json;
using Repackinator.Shared;

namespace RepackinatorUI
{
    public class Config
    {
        public string InputPath { get; set; } = string.Empty;

        public string OutputPath { get; set; } = string.Empty;
        
        public string TempPath { get; set; } = Path.GetTempPath();

        public bool Alternative { get; set; } = false;

        public static Config LoadConfig(string path)
        {
            var configJson = File.ReadAllText(path);
            var result = JsonConvert.DeserializeObject<Config>(configJson);
            return result ?? new Config();
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

            var result = JsonConvert.SerializeObject(config);
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

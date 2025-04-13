using System.Globalization;
using System.Text.Json;
using Repackinator.Core.Helpers;

namespace Repackinator.Core.Models
{
    public static class GameDataHelper
    {
        public static string fix(string tofix, string region)
        {
            var textinfo = new CultureInfo("en-US", false).TextInfo;
            tofix = textinfo.ToTitleCase(tofix.ToLower());
            tofix = tofix.Replace("FC", "FC", StringComparison.CurrentCultureIgnoreCase);
            tofix = tofix.Replace("II", "II", StringComparison.CurrentCultureIgnoreCase);
            tofix = tofix.Replace($"({region})", $"({region})", StringComparison.CurrentCultureIgnoreCase);
            return tofix;
        }

        public static GameData[] LoadGameData(string path)
        {
            var gameDataJson = File.ReadAllText(path);
            var result = JsonSerializer.Deserialize<GameData[]>(gameDataJson) ?? [];
            for (int i = 0; i < result.Length; i++)
            {
                result[i].Index = i;
            }
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

        public static void SaveGameData(string path, GameData[]? gameData)
        {
            if (gameData == null)
            {
                return;
            }

            var result = JsonSerializer.Serialize(gameData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, result);
        }

        public static void SaveGameData(GameData[]? gameData)
        {
            var applicationPath = Utility.GetApplicationPath();
            if (applicationPath == null)
            {
                return;
            }

            var repackListPath = Path.Combine(applicationPath, "RepackList.json");
            SaveGameData(repackListPath, gameData);
        }
    }
}

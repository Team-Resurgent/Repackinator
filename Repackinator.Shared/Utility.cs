using System.Security.Principal;

namespace Repackinator.Shared
{
    public static class Utility
    {
        public static bool IsAdmin()
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }
            bool isAdmin;
            try
            {
                var user = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch 
            {
                isAdmin = false;
            }
            return isAdmin;
        }

        public static string[] GetSlicesFromFile(string filename)
        {
            var slices = new List<string>();            
            var extension = Path.GetExtension(filename);
            var fileWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            var subExtension = Path.GetExtension(fileWithoutExtension);
            if (subExtension.Equals(".1") || subExtension.Equals(".2"))
            {
                var fileWithoutSubExtension = Path.GetFileNameWithoutExtension(fileWithoutExtension);
                var directory = Path.GetDirectoryName(filename);
                if (directory != null)
                {
                    for (var i = 1; i <= 2; i++)
                    {
                        var fileToAdd = Path.Combine(directory, $"{fileWithoutSubExtension}.{i}{extension}");
                        if (File.Exists(fileToAdd))
                        {
                            slices.Add(fileToAdd);
                        }
                    }
                }
            }
            else
            {
                slices.Add(filename);
            }
            slices.Sort();
            return slices.ToArray();
        }

        public static string? GetApplicationPath()
        {
            var exePath = AppDomain.CurrentDomain.BaseDirectory;
            if (exePath == null)
            {
                return null;
            }

            var result = Path.GetDirectoryName(exePath);
            return result;
        }

        public static string FormatLogMessage(LogMessage logMessage)
        {
            if (logMessage.Level == LogMessageLevel.None)
            {
                return "\n";
            }
            var formattedTime = logMessage.Time.ToString("HH:mm:ss");
            var message = $"{formattedTime} {logMessage.Level} - {logMessage.Message}";
            return $"{message}\n";
        }
    }
}

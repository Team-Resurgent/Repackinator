using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;

namespace Repackinator.Core.Helpers
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

        /// <summary>
        /// Restarts the current process with administrator privileges.
        /// Returns true if the process was restarted, false if elevation failed or is not needed.
        /// </summary>
        public static bool RestartAsAdmin(string[] args)
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            if (IsAdmin())
            {
                return false; // Already admin, no need to restart
            }

            try
            {
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath))
                {
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = string.Join(" ", args.Select(arg => arg.Contains(' ') ? $"\"{arg}\"" : arg)),
                    UseShellExecute = true,
                    Verb = "runas" // This triggers UAC elevation
                };

                Process.Start(startInfo);
                return true; // Successfully started elevated process
            }
            catch
            {
                // User likely cancelled UAC prompt or elevation failed
                return false;
            }
        }

        public static string GetNameFromSlice(string filename)
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            var subExtension = Path.GetExtension(nameWithoutExtension);
            if (subExtension.Equals(".1") || subExtension.Equals(".2"))
            {
                nameWithoutExtension = Path.GetFileNameWithoutExtension(nameWithoutExtension);
            }
            return nameWithoutExtension;
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

        public static string EnumValueToString(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr = field?.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>().FirstOrDefault();
            return attr?.Description ?? value.ToString();
        }

        public static T StringValueToEnum<T>(string value) where T : struct, Enum
        {
            foreach (T enumValue in Enum.GetValues(typeof(T)))
            {
                var enumString = enumValue.ToString() ?? "";
                var field = value.GetType().GetField(enumString);
                var attr = field?.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>().FirstOrDefault();
                if (attr?.Description.Equals(value) ?? false || enumString.Equals(value))
                {
                    return enumValue;
                }
            }
            throw new ArgumentException($"The value '{value}' is not a valid value for enum type {typeof(T).Name}.", nameof(value));
        }

        public static bool ValidateFatX(string value)
        {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz 0123456789!#$%&'()-.@[]^_`{}~";
            foreach (var c in value)
            {
                if (!validChars.Contains(c))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

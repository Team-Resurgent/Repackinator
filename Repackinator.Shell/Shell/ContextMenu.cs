using Microsoft.Win32;
using Repackinator.Core.Helpers;

namespace Repackinator.Shell.Shell
{
    public class ContextMenu
    {
        private static void RegisterSubMenu(RegistryKey key, string name, string description, string command, string iconPath)
        {
            if (OperatingSystem.IsWindows() && Utility.IsAdmin())
            {
                var menu1Key = key.CreateSubKey($"shell\\{name}");
                menu1Key.SetValue("MUIVerb", description);
                menu1Key.SetValue("Icon", $"{iconPath},0");
                var commandMenu1Key = menu1Key.CreateSubKey("command");
                commandMenu1Key.SetValue(null, command);
            }
        }

        public static bool RegisterContext()
        {
            if (OperatingSystem.IsWindows() && Utility.IsAdmin())
            {
                // Always unregister first to clear old menus
                UnregisterContext();

                // Use shell executable for command-line actions
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var shellExePath = Path.Combine(baseDir, "repackinator.shell.exe");
                
                // Fallback to main exe if shell doesn't exist (for backwards compatibility)
                var exePath = File.Exists(shellExePath) ? shellExePath : Path.Combine(baseDir, $"{AppDomain.CurrentDomain.FriendlyName}.exe");

                using var key = Registry.ClassesRoot.CreateSubKey($"*\\shell\\Repackinator");

                key.SetValue("AppliesTo", ".iso OR .cci");
                key.SetValue("MUIVerb", "Repackinator");
                key.SetValue("SubCommands", string.Empty);
                key.SetValue("Icon", $"{exePath},0");
                key.SetValue("Version", Core.Version.Value);

                // Convert to ISO options
                RegisterSubMenu(key, "01ConvertToISO", "Convert to ISO", $"\"{exePath}\" -a=convert -i \"%L\" -w", exePath);
                RegisterSubMenu(key, "02ConvertToISOScrub", "Convert to ISO (Scrub)", $"\"{exePath}\" -a=convert -i \"%L\" -s -w", exePath);
                RegisterSubMenu(key, "03ConvertToISOTrimScrub", "Convert to ISO (TrimScrub)", $"\"{exePath}\" -a=convert -i \"%L\" -t -w", exePath);

                // Convert to CCI options
                RegisterSubMenu(key, "04ConvertToCCI", "Convert to CCI", $"\"{exePath}\" -a=convert -i \"%L\" -c -w", exePath);
                RegisterSubMenu(key, "05ConvertToCCIScrub", "Convert to CCI (Scrub)", $"\"{exePath}\" -a=convert -i \"%L\" -s -c -w", exePath);
                RegisterSubMenu(key, "06ConvertToCCITrimScrub", "Convert to CCI (TrimScrub)", $"\"{exePath}\" -a=convert -i \"%L\" -t -c -w", exePath);

                // Information and extraction options
                RegisterSubMenu(key, "07XbeInfo", "XBE Info", $"\"{exePath}\" -a=xbeinfo -i \"%L\" -w", exePath);
                RegisterSubMenu(key, "08Info", "Sector Info", $"\"{exePath}\" -a=info -i \"%L\" -w", exePath);
                RegisterSubMenu(key, "09Checksum", "Checksum (SHA256)", $"\"{exePath}\" -a=checksum -i \"%L\" -w", exePath);
                RegisterSubMenu(key, "10Extract", "Extract Files", $"\"{exePath}\" -a=extract -i \"%L\" -w", exePath);

                // Compare options
                RegisterSubMenu(key, "11CompareSetFirst", "Compare - Set First", $"\"{exePath}\" -a=compare -f \"%L\"", exePath);
                RegisterSubMenu(key, "12CompareFirstWith", "Compare - First With This", $"\"{exePath}\" -a=compare -s \"%L\" -c -w", exePath);

                // Register context menu for folders (Directory)
                // (UnregisterFolderContext was already called by UnregisterContext above)
                using var dirKey = Registry.ClassesRoot.CreateSubKey($"Directory\\shell\\Repackinator");

                dirKey.SetValue("MUIVerb", "Repackinator");
                dirKey.SetValue("SubCommands", string.Empty);
                dirKey.SetValue("Icon", $"{exePath},0");
                dirKey.SetValue("Version", Core.Version.Value);

                // Pack To ISO - output will be in parent directory with folder name
                RegisterSubMenu(dirKey, "01PackToISO", "Pack To ISO", $"\"{exePath}\" -a=pack -i \"%1\" -o \"%1.iso\" -w", exePath);
                
                // Pack To CCI - output will be in parent directory with folder name
                RegisterSubMenu(dirKey, "02PackToCCI", "Pack To CCI", $"\"{exePath}\" -a=pack -i \"%1\" -o \"%1.cci\" -c -w", exePath);

                return true;
            }

            return false;
        }

        public static bool UnregisterContext()
        {
            if (OperatingSystem.IsWindows() && Utility.IsAdmin())
            {
                Registry.ClassesRoot.DeleteSubKeyTree("*\\shell\\Repackinator", false);
                UnregisterFolderContext();
                return true;
            }

            return false;
        }

        private static bool UnregisterFolderContext()
        {
            if (OperatingSystem.IsWindows() && Utility.IsAdmin())
            {
                Registry.ClassesRoot.DeleteSubKeyTree("Directory\\shell\\Repackinator", false);
                return true;
            }

            return false;
        }
    }
}


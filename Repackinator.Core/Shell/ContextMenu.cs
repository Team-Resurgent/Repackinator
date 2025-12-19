using Microsoft.Win32;
using Repackinator.Core.Helpers;

namespace Repackinator.Core.Shell
{
    public class ContextMenu
    {
        private static void RegisterSubMenu(RegistryKey key, string name, string description, string command)
        {
            if (OperatingSystem.IsWindows() && Utility.IsAdmin())
            {
                var menu1Key = key.CreateSubKey($"shell\\{name}");
                menu1Key.SetValue("MUIVerb", description);
                var commandMenu1Key = menu1Key.CreateSubKey("command");
                commandMenu1Key.SetValue(null, command);
            }
        }

        public static bool RegisterContext()
        {
            if (OperatingSystem.IsWindows() && Utility.IsAdmin())
            {
                var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{AppDomain.CurrentDomain.FriendlyName}.exe");

                // Register context menu for files (*)
                using var tempKey = Registry.ClassesRoot.OpenSubKey($"*\\shell\\Repackinator");
                if ((string?)tempKey?.GetValue("Version") != Version.Value)
                {
                    tempKey?.Close();
                    UnregisterContext();
                }
                else
                {
                    tempKey?.Close();
                }

                using var key = Registry.ClassesRoot.CreateSubKey($"*\\shell\\Repackinator");

                key.SetValue("AppliesTo", ".iso OR .cci");
                key.SetValue("MUIVerb", "Repackinator");
                key.SetValue("SubCommands", string.Empty);
                key.SetValue("Version", Version.Value);

                RegisterSubMenu(key, "01ConvertToISO", "Convert to ISO", $"\"{exePath}\" -a=convert -i \"%L\" -w");
                RegisterSubMenu(key, "02ConvertToISOScrub", "Convert to ISO (Scrub)", $"\"{exePath}\" -a=convert -i \"%L\" -s -w");
                RegisterSubMenu(key, "03ConvertToISOTrimScrub", "Convert to ISO (TrimScrub)", $"\"{exePath}\" -a=convert -i \"%L\" -t -w");

                RegisterSubMenu(key, "04ConvertToCCI", "Convert to CCI", $"\"{exePath}\" -a=convert -i \"%L\" -c -w");
                RegisterSubMenu(key, "05ConvertToCCIScrub", "Convert to CCI (Scrub)", $"\"{exePath}\" -a=convert -i \"%L\" -s -c -w");
                RegisterSubMenu(key, "06ConvertToCCITrimScrub", "Convert to CCI (TrimScrub)", $"\"{exePath}\" -a=convert -i \"%L\" -t -c -w");

                RegisterSubMenu(key, "10CompareSetFirst", "Compare Set First", $"\"{exePath}\" -a=compare -f \"%L\"");
                RegisterSubMenu(key, "11CompareFirstWith", "Compare First With", $"\"{exePath}\" -a=compare -s \"%L\" -c -w");
                RegisterSubMenu(key, "12Info", "Info", $"\"{exePath}\" -a=info -i \"%L\" -w");
                RegisterSubMenu(key, "13ChecksumSectorData", "Checksum Sector Data (SHA256)", $"\"{exePath}\" -a=checksum -i \"%L\" -w");
                RegisterSubMenu(key, "14Extract", "Extract", $"\"{exePath}\" -a=extract -i \"%L\" -w");

                // Register context menu for folders (Directory)
                using var tempDirKey = Registry.ClassesRoot.OpenSubKey($"Directory\\shell\\Repackinator");
                if ((string?)tempDirKey?.GetValue("Version") != Version.Value)
                {
                    tempDirKey?.Close();
                    UnregisterFolderContext();
                }
                else
                {
                    tempDirKey?.Close();
                }

                using var dirKey = Registry.ClassesRoot.CreateSubKey($"Directory\\shell\\Repackinator");

                dirKey.SetValue("MUIVerb", "Repackinator");
                dirKey.SetValue("SubCommands", string.Empty);
                dirKey.SetValue("Version", Version.Value);

                // Pack To ISO - output will be in parent directory with folder name
                RegisterSubMenu(dirKey, "01PackToISO", "Pack To ISO", $"\"{exePath}\" -a=pack -i \"%1\" -o \"%1.iso\" -w");
                
                // Pack To CCI - output will be in parent directory with folder name
                RegisterSubMenu(dirKey, "02PackToCCI", "Pack To CCI", $"\"{exePath}\" -a=pack -i \"%1\" -o \"%1.cci\" -c -w");

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

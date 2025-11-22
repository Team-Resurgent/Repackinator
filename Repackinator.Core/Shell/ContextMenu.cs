using Microsoft.Win32;
using Repackinator.Core.Helpers;

namespace Repackinator.Core.Shell
{
    public class ContextMenu
    {
        private static void RegisterSubMenu(RegistryKey key, string name, string description, string command)
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }
            var menu1Key = key.CreateSubKey($"shell\\{name}");
            menu1Key.SetValue("MUIVerb", description);
            var commandMenu1Key = menu1Key.CreateSubKey("command");
            commandMenu1Key.SetValue(null, command);
        }

        public static bool RegisterContext()
        {
            if (!OperatingSystem.IsWindows() || !Utility.IsAdmin())
            {
                return false;
            }
            var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{AppDomain.CurrentDomain.FriendlyName}.exe");

            using var key = Registry.ClassesRoot.CreateSubKey($"*\\shell\\Repackinator");

            key.SetValue("AppliseTo", ".iso OR .cci");
            key.SetValue("MUIVerb", "Repackinator");
            key.SetValue("SubCommands", string.Empty);

            RegisterSubMenu(key, "01ConvertToISO", "Convert To ISO", $"\"{exePath}\" -i=\"%L\" -a=convert -w");
            RegisterSubMenu(key, "02ConvertToISOScrub", "Convert To ISO (Scrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=Scrub -w");
            RegisterSubMenu(key, "03ConvertToISOTrimScrub", "Convert To ISO (TrimScrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=TrimScrub -w");

            RegisterSubMenu(key, "04ConvertToCCI", "Convert To CCI", $"\"{exePath}\" -i=\"%L\" -a=convert -c=CCI -w");
            RegisterSubMenu(key, "05ConvertToCCIScrub", "Convert To CCI (Scrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=Scrub -c=CCI -w");
            RegisterSubMenu(key, "06ConvertToCCITrimScrub", "Convert To CCI (TrimScrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=TrimScrub -c=CCI -w");

            //

            RegisterSubMenu(key, "10CompareSetFirst", "Compare Set First", $"\"{exePath}\" -f=\"%L\" -a=compare");
            RegisterSubMenu(key, "11CompareFirstWith", "Compare First With", $"\"{exePath}\" -s=\"%L\" -a=compare -c -w");
            RegisterSubMenu(key, "12Info", "Info", $"\"{exePath}\" -i=\"%L\" -a=info -w");
            RegisterSubMenu(key, "13ChecksumSectorData", "Checksum Sector Data (SHA256)", $"\"{exePath}\" -i=\"%L\" -a=checksum -w");
            RegisterSubMenu(key, "14Extract", "Extract", $"\"{exePath}\" -i=\"%L\" -a=extract -w");

            return true;
        }

        public static bool UnregisterContext()
        {
            if (!OperatingSystem.IsWindows() || !Utility.IsAdmin())
            {
                return false;
            }
            Registry.ClassesRoot.DeleteSubKeyTree("*\\shell\\Repackinator");
            return true;
        }
    }
}

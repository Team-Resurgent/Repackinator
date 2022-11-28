using Microsoft.Win32;
using Repackinator.Shared;

namespace Repackinator
{
    public class ShellExtension
    {
        private static void RegisterSubMenu(RegistryKey key, string name, string description, string command, string extension)
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }
            var menu1Key = key.CreateSubKey($"shell\\{name}");
            menu1Key.SetValue("MUIVerb", description);
            menu1Key.SetValue("AppliesTo", extension);
            var commandMenu1Key = menu1Key.CreateSubKey("command");
            commandMenu1Key.SetValue(null, command);
        }

        public static void RegisterContext()
        {
            if (!OperatingSystem.IsWindows() || !Utility.IsAdmin())
            {
                return;
            }
            var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{AppDomain.CurrentDomain.FriendlyName}.exe");
            var command = $"\"{exePath}\" \"%L\"";

            using (var key = Registry.ClassesRoot.CreateSubKey($"*\\shell\\Repackinator"))
            {
                key.SetValue("MUIVerb", "Repackinator");
                key.SetValue("SubCommands", string.Empty);
                RegisterSubMenu(key, "01menu", "Convert To ISO", command, ".iso");
                RegisterSubMenu(key, "02menu", "Convert To ISO (Scrub)", command, ".iso");
                RegisterSubMenu(key, "03menu", "Convert To ISO (Scrub+Truncate)", command, ".iso");
                RegisterSubMenu(key, "04menu", "Convert To CCI", command, ".iso");
                RegisterSubMenu(key, "05menu", "Convert To CCI (Scrub)", command, ".iso");
                RegisterSubMenu(key, "06menu", "Convert To CCI (Scrub+Truncate)", command, ".iso");
                
                RegisterSubMenu(key, "07menu", "Convert To ISO", command, ".cci");
                RegisterSubMenu(key, "08menu", "Convert To ISO (Scrub)", command, ".cci");
                RegisterSubMenu(key, "09menu", "Convert To ISO (Scrub+Truncate)", command, ".cci");
                RegisterSubMenu(key, "10menu", "Convert To CCI", command, ".cci");
                RegisterSubMenu(key, "11menu", "Convert To CCI (Scrub)", command, ".cci");
                RegisterSubMenu(key, "12menu", "Convert To CCI (Scrub+Truncate)", command, ".cci");
            }
        }

        public static void UnregisterContext()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }
            Registry.ClassesRoot.DeleteSubKeyTree("*\\shell\\Repackinator");
        }
    }
}

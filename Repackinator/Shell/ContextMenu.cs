using Microsoft.Win32;
using Repackinator.Helpers;

namespace Repackinator.Shell
{
    public class ContextMenu
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

        public static bool RegisterContext()
        {
            if (!OperatingSystem.IsWindows() || !Utility.IsAdmin())
            {
                return false;
            }
            var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{AppDomain.CurrentDomain.FriendlyName}.exe");

            using var key = Registry.ClassesRoot.CreateSubKey($"*\\shell\\Repackinator");
            
            key.SetValue("MUIVerb", "Repackinator");
            key.SetValue("SubCommands", string.Empty);

            RegisterSubMenu(key, "01menu", "Convert To ISO", $"\"{exePath}\" -i=\"%L\" -a=convert -w", ".iso");
            RegisterSubMenu(key, "02menu", "Convert To ISO (Scrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=Scrub -w", ".iso");
            RegisterSubMenu(key, "03menu", "Convert To ISO (TrimmedScrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=TrimmedScrub -w", ".iso");

            RegisterSubMenu(key, "04menu", "Convert To CCI", $"\"{exePath}\" -i=\"%L\" -a=convert -c=CCI -w", ".iso");
            RegisterSubMenu(key, "05menu", "Convert To CCI (Scrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=Scrub -c=CCI -w", ".iso");
            RegisterSubMenu(key, "06menu", "Convert To CCI (TrimmedScrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=TrimmedScrub -c=CCI -w", ".iso");

            RegisterSubMenu(key, "07menu", "Convert To CSO", $"\"{exePath}\" -i=\"%L\" -a=convert -c=CSO -w", ".iso");
            RegisterSubMenu(key, "08menu", "Convert To CSO (Scrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=Scrub -c=CSO -w", ".iso");
            RegisterSubMenu(key, "09menu", "Convert To CSO (TrimmedScrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=TrimmedScrub -c=CSO -w", ".iso");

            //

            RegisterSubMenu(key, "10menu", "Convert To ISO", $"\"{exePath}\" -i=\"%L\" -a=convert -w", ".cci");
            RegisterSubMenu(key, "11menu", "Convert To ISO (Scrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=Scrub -w", ".cci");
            RegisterSubMenu(key, "12menu", "Convert To ISO (TrimmedScrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=TrimmedScrub -w", ".cci");

            RegisterSubMenu(key, "13menu", "Convert To CCI", $"\"{exePath}\" -i=\"%L\" -a=convert -c=CCI -w", ".cci");
            RegisterSubMenu(key, "14menu", "Convert To CCI (Scrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=Scrub -c=CCI -w", ".cci");
            RegisterSubMenu(key, "15menu", "Convert To CCI (TrimmedScrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=TrimmedScrub -c=CCI -w", ".cci");

            RegisterSubMenu(key, "16menu", "Convert To CSO", $"\"{exePath}\" -i=\"%L\" -a=convert -c=CSO -w", ".cci");
            RegisterSubMenu(key, "17menu", "Convert To CSO (Scrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=Scrub -c=CSO -w", ".cci");
            RegisterSubMenu(key, "18menu", "Convert To CSO (TrimmedScrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=TrimmedScrub -c=CSO -w", ".cci");

            //

            RegisterSubMenu(key, "19menu", "Convert To ISO", $"\"{exePath}\" -i=\"%L\" -a=convert -w", ".cso");
            RegisterSubMenu(key, "20menu", "Convert To ISO (Scrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=Scrub -w", ".cso");
            RegisterSubMenu(key, "21menu", "Convert To ISO (TrimmedScrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=TrimmedScrub -w", ".cso");

            RegisterSubMenu(key, "22menu", "Convert To CCI", $"\"{exePath}\" -i=\"%L\" -a=convert -c=CCI -w", ".cso");
            RegisterSubMenu(key, "23menu", "Convert To CCI (Scrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=Scrub -c=CCI -w", ".cso");
            RegisterSubMenu(key, "24menu", "Convert To CCI (TrimmedScrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=TrimmedScrub -c=CCI -w", ".cso");

            RegisterSubMenu(key, "25menu", "Convert To CSO", $"\"{exePath}\" -i=\"%L\" -a=convert -c=CSO -w", ".cso");
            RegisterSubMenu(key, "26menu", "Convert To CSO (Scrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=Scrub -c=CSO -w", ".cso");
            RegisterSubMenu(key, "27menu", "Convert To CSO (TrimmedScrub)", $"\"{exePath}\" -i=\"%L\" -a=convert -s=TrimmedScrub -c=CSO -w", ".cso");

            //

            RegisterSubMenu(key, "28menu", "Compare Set First", $"\"{exePath}\" -f=\"%L\" -a=compare", ".iso");
            RegisterSubMenu(key, "29menu", "Compare First With", $"\"{exePath}\" -s=\"%L\" -a=compare -c -w", ".iso");
            RegisterSubMenu(key, "30menu", "Compare Set First", $"\"{exePath}\" -f=\"%L\" -a=compare", ".cci");
            RegisterSubMenu(key, "31menu", "Compare First With", $"\"{exePath}\" -s=\"%L\" -a=compare -c -w", ".cci");
            RegisterSubMenu(key, "32menu", "Compare Set First", $"\"{exePath}\" -f=\"%L\" -a=compare", ".cso");
            RegisterSubMenu(key, "33menu", "Compare First With", $"\"{exePath}\" -s=\"%L\" -a=compare -c -w", ".cso");

            RegisterSubMenu(key, "34menu", "Info", $"\"{exePath}\" -i=\"%L\" -a=info -w", ".iso");
            RegisterSubMenu(key, "35menu", "Info", $"\"{exePath}\" -i=\"%L\" -a=info -w", ".cci");
            RegisterSubMenu(key, "36menu", "Info", $"\"{exePath}\" -i=\"%L\" -a=info -w", ".cso");

            RegisterSubMenu(key, "37menu", "Checksum Sector Data (SHA256)", $"\"{exePath}\" -i=\"%L\" -a=checksum -w", ".iso");
            RegisterSubMenu(key, "38menu", "Checksum Sector Data (SHA256)", $"\"{exePath}\" -i=\"%L\" -a=checksum -w", ".cci");
            RegisterSubMenu(key, "39menu", "Checksum Sector Data (SHA256)", $"\"{exePath}\" -i=\"%L\" -a=checksum -w", ".cso");

            RegisterSubMenu(key, "40menu", "Extract", $"\"{exePath}\" -i=\"%L\" -a=extract -w", ".iso");
            RegisterSubMenu(key, "41menu", "Extract", $"\"{exePath}\" -i=\"%L\" -a=extract -w", ".cci");
            RegisterSubMenu(key, "42menu", "Extract", $"\"{exePath}\" -i=\"%L\" -a=extract -w", ".cso");

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

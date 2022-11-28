using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace RepackinatorUI
{
    public class ShellExtension
    {
        private static string FileTypeRepackinatorISO = "Repackinator.ISO";
        private static string FileTypeRepackinatorCCI = "Repackinator.CCI";

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

        private static string BuildFilter(string[]? extensions)
        {
            if (extensions == null || extensions.Length == 0)
            {
                return "*";
            }
            var stringBuilder = new StringBuilder();            
            for (int i = 0; i < extensions.Length; i++) 
            {
                if (i > 0)
                {
                    stringBuilder.Append(" OR ");
                }
                stringBuilder.Append($"System.FileName:\"*.{extensions[i]}\"");                
            }
            return stringBuilder.ToString();
        }

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

        private static void Register(string fileType, string shellKeyName, string menuText, string menuCommand)
        {
            if (!OperatingSystem.IsWindows())
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

        private static void Unregister(string fileType, string shellKeyName)
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }
            Registry.ClassesRoot.DeleteSubKeyTree("*\\shell\\Repackinator");
        }

        public static void RegisterContext()
        {
            if (!OperatingSystem.IsWindows() || !IsAdmin())
            {
                return;
            }
            var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{AppDomain.CurrentDomain.FriendlyName}.exe");
            var command = $"\"{exePath}\" \"%L\"";
            Register(FileTypeRepackinatorISO, "Repackinator", "Enable Scrub", command);
        }

        public static void UnregisterContext()
        {
            if (!OperatingSystem.IsWindows() || !IsAdmin())
            {
                return;
            }
            //Unregister("jpegfile", "Simple Context Menu");
        }
        //        string menuCommand = string.Format("\"{0}\" \"%L\"",
        //                                   Application.ExecutablePath);
        //        FileShellExtension.Register("jpegfile", "Simple Context Menu", 
        //                            "Copy to Grayscale", menuCommand);

        //// sample usage to unregister
        //FileShellExtension.Unregister("jpegfile", "Simple Context Menu");
    }
}

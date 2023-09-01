﻿using System.Security.Principal;

namespace Repackinator.Helpers
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
    }
}

using Mono.Options;

namespace Repackinator.Shell.Console
{
    public static class ConsoleUtil
    {
        private const string contributors = "HoRnEyDvL, Hazeno, Rocky5, navi, Fredr1kh, Natetronn, Incursion64, Zatchbot, Team Cerbios";
        public static void ShowHelpHeader(string version, OptionSet options, Dictionary<string, string> actionDescriptions)
        {
            System.Console.WriteLine($"Repackinator {version}");
            System.Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
            System.Console.WriteLine($"Credits go to {contributors}.");
            System.Console.WriteLine();
            System.Console.WriteLine("Usage: Repackinator -a=<action> [options]+");
            System.Console.WriteLine();
            System.Console.WriteLine("Available Actions:");
            System.Console.WriteLine();
            foreach (var action in actionDescriptions)
            {
                System.Console.WriteLine($"  {action.Key,-15} {action.Value}");
            }
            System.Console.WriteLine();
            System.Console.WriteLine("Global Options:");
            System.Console.WriteLine();
            options.WriteOptionDescriptions(System.Console.Out);
            System.Console.WriteLine();
            System.Console.WriteLine("For detailed help on a specific action, use:");
            System.Console.WriteLine("  Repackinator -a=<action> -h");
            System.Console.WriteLine();
            System.Console.WriteLine("Examples:");
            System.Console.WriteLine("  Repackinator -a=pack -h          Show help for Pack action");
            System.Console.WriteLine("  Repackinator -a=convert -h        Show help for Convert action");
            System.Console.WriteLine("  Repackinator -a=repack -h         Show help for Repack action");
        }

        public static void ShowHelpHeaderForAction(string version, string action, OptionSet options)
        {
            System.Console.WriteLine($"Repackinator {version}");
            System.Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
            System.Console.WriteLine($"Credits go to {contributors}.");
            System.Console.WriteLine();
            System.Console.WriteLine($"Usage: Repackinator --action={action} [options]+");
            System.Console.WriteLine();
            options.WriteOptionDescriptions(System.Console.Out);
        }

        public static void ShowOptionException(OptionException optionException, string? action = null, string? version = null)
        {
            System.Console.Write("Repackinator by EqUiNoX: ");
            System.Console.WriteLine(optionException.Message);
            
            if (!string.IsNullOrEmpty(action) && !string.IsNullOrEmpty(version))
            {
                System.Console.WriteLine();
                System.Console.WriteLine($"Showing help for '{action}' action:");
                System.Console.WriteLine();
                ShowActionHelp(action, version);
            }
            else
            {
                System.Console.WriteLine("Try `Repackinator --help' for more information.");
            }
        }

        private static void ShowActionHelp(string action, string version)
        {
            var actionLower = action.ToLowerInvariant();
            OptionSet? options = null;
            string actionName = action;

            switch (actionLower)
            {
                case "pack":
                    options = ConsolePack.GetOptions();
                    actionName = ConsolePack.Action;
                    break;
                case "repack":
                    options = ConsoleRepack.GetOptions();
                    actionName = ConsoleRepack.Action;
                    break;
                case "convert":
                    options = ConsoleConvert.GetOptions();
                    actionName = ConsoleConvert.Action;
                    break;
                case "extract":
                    options = ConsoleExtract.GetOptions();
                    actionName = ConsoleExtract.Action;
                    break;
                case "compare":
                    options = ConsoleCompare.GetOptions();
                    actionName = ConsoleCompare.Action;
                    break;
                case "info":
                    options = ConsoleInfo.GetOptions();
                    actionName = ConsoleInfo.Action;
                    break;
                case "checksum":
                    options = ConsoleChecksum.GetOptions();
                    actionName = ConsoleChecksum.Action;
                    break;
                case "xbeinfo":
                    options = ConsoleXbeInfo.GetOptions();
                    actionName = ConsoleXbeInfo.Action;
                    break;
                case "register":
                    if (OperatingSystem.IsWindows())
                    {
                        options = ConsoleRegister.GetOptions();
                        actionName = ConsoleRegister.Action;
                    }
                    break;
                case "unregister":
                    if (OperatingSystem.IsWindows())
                    {
                        options = ConsoleUnregister.GetOptions();
                        actionName = ConsoleUnregister.Action;
                    }
                    break;
            }

            if (options != null)
            {
                ShowHelpHeaderForAction(version, actionName, options);
            }
            else
            {
                System.Console.WriteLine($"Unknown action: {action}");
                System.Console.WriteLine("Try `Repackinator --help' for more information.");
            }
        }

        public static void ProcessWait(bool wait)
        {
            if (!wait)
            {
                return;
            }
            System.Console.WriteLine();
            System.Console.Write("Press any key to continue.");
            System.Console.ReadKey();
        }
    }
}


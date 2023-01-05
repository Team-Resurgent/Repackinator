using Mono.Options;
using Repackinator.Shell;

namespace Repackinator.Console
{
    public static class ConsoleStartup
    {
        public static string Action { get; set; } = string.Empty;
        public static bool ShowHelp { get; set; } = false;
        public static bool Wait { get; set; } = false;

        private static string ActionRegister = "Register";
        private static string ActionUnregister = "Unregister";
        private static string ActionConvert = "Convert";
        private static string ActionCompare = "Compare";
        private static string ActionInfo = "Info";
        private static string ActionChecksum = "Checksum";
        private static string ActionExtract = "Extract";
        private static string ActionRepack = "Repack";

        public static void Process(string version, string[] args)
        {
            var actions = OperatingSystem.IsWindows() ? "Register, Unregister, Convert, Compare, Info, Checksum, Extract, Repack" : "Convert, Compare, Info, Checksum, Extract, Repack";

            var options = new OptionSet {
                { "a|action=", $"Action ({actions})", a => Action = a },
                { "h|help", "show help", h => ShowHelp = h != null },
                { "w|wait", "Wait on exit", w => Wait = w != null }
            };

            try
            {
                options.Parse(args);
                if (ShowHelp && args.Length == 1)
                {
                    ConsoleUtil.ShowHelpHeader(version);
                    options.WriteOptionDescriptions(System.Console.Out);

                    if (OperatingSystem.IsWindows())
                    {
                        System.Console.WriteLine();
                        System.Console.WriteLine("Register Action...");
                        System.Console.WriteLine();
                        System.Console.WriteLine("This action adds or updates repackinor's context menu (needs admin privileges).");
                        System.Console.WriteLine();
                        ConsoleUnregister.ShowOptionDescription();
                    }

                    if (OperatingSystem.IsWindows())
                    {
                        System.Console.WriteLine();
                        System.Console.WriteLine("Unregister Action...");
                        System.Console.WriteLine();
                        System.Console.WriteLine("This action removes repackinor's context menu  (needs admin privileges).");
                        System.Console.WriteLine();
                        ConsoleUnregister.ShowOptionDescription();
                    }

                    System.Console.WriteLine();
                    System.Console.WriteLine("Convert Action...");
                    System.Console.WriteLine();
                    System.Console.WriteLine("This action is used to convert one xbox disk image format to another.");
                    System.Console.WriteLine();
                    ConsoleConvert.ShowOptionDescription();

                    System.Console.WriteLine();
                    System.Console.WriteLine("Compare Action...");
                    System.Console.WriteLine();
                    System.Console.WriteLine("This action is used to compare one xbox disk image with another.");
                    System.Console.WriteLine();
                    ConsoleCompare.ShowOptionDescription();

                    System.Console.WriteLine();
                    System.Console.WriteLine("Info Action...");
                    System.Console.WriteLine();
                    System.Console.WriteLine("This action is used to show xbox disk data sector information.");
                    System.Console.WriteLine();
                    ConsoleInfo.ShowOptionDescription();

                    System.Console.WriteLine();
                    System.Console.WriteLine("Checksum Action...");
                    System.Console.WriteLine();
                    System.Console.WriteLine("This action is used to checksum xbox disk image sectors after any decompression if applicable.");
                    System.Console.WriteLine();
                    ConsoleChecksum.ShowOptionDescription();

                    System.Console.WriteLine();
                    System.Console.WriteLine("Extract Action...");
                    System.Console.WriteLine();
                    System.Console.WriteLine("This action is used to extract files from xbox disk image.");
                    System.Console.WriteLine();
                    ConsoleExtract.ShowOptionDescription();

                    System.Console.WriteLine();
                    System.Console.WriteLine("Repack Action...");
                    System.Console.WriteLine();
                    System.Console.WriteLine("This action is used to repackinate your collection of xbox disk images.");
                    System.Console.WriteLine();
                    ConsoleRepack.ShowOptionDescription();

                    ConsoleUtil.ProcessWait(Wait);
                    return;
                }

                if (OperatingSystem.IsWindows() && Action.Equals(ActionRegister, StringComparison.CurrentCultureIgnoreCase))
                {
                    ConsoleRegister.Process(version, args);
                }
                if (OperatingSystem.IsWindows() && Action.Equals(ActionUnregister, StringComparison.CurrentCultureIgnoreCase))
                {
                    ConsoleUnregister.Process(version, args);
                }
                else if (Action.Equals(ActionConvert, StringComparison.CurrentCultureIgnoreCase))
                {
                    ConsoleConvert.Process(version, args);
                }
                else if (Action.Equals(ActionCompare, StringComparison.CurrentCultureIgnoreCase))
                {
                    ConsoleCompare.Process(version, args);
                }
                else if (Action.Equals(ActionInfo, StringComparison.CurrentCultureIgnoreCase))
                {
                    ConsoleInfo.Process(version, args);
                }
                else if (Action.Equals(ActionChecksum, StringComparison.CurrentCultureIgnoreCase))
                {
                    ConsoleChecksum.Process(version, args);
                }
                else if (Action.Equals(ActionExtract, StringComparison.CurrentCultureIgnoreCase))
                {
                    ConsoleExtract.Process(version, args);
                }
                else if (Action.Equals(ActionRepack, StringComparison.CurrentCultureIgnoreCase))
                {
                    ConsoleRepack.Process(version, args);
                }
                else
                {
                    throw new OptionException("Action is not valid.", "action");
                }
            }
            catch (OptionException e)
            {
                ConsoleUtil.ShowOptionException(e);
            }

            ConsoleUtil.ProcessWait(Wait);
        }
    }
}

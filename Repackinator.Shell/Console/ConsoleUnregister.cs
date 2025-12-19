using Mono.Options;
using Repackinator.Core.Shell;
using Repackinator.Core.Helpers;

namespace Repackinator.Shell.Console
{
    public static class ConsoleUnregister
    {
        public const string Action = "Unregister";
        public static bool ShowHelp { get; set; } = false;
        public static bool Wait { get; set; } = false;

        public static OptionSet GetOptions()
        {
            return new OptionSet {
                { "h|help", "show help", h => ShowHelp = h != null },
                { "w|wait", "Wait on exit", w => Wait = w != null }
            };
        }

        public static void ShowOptionDescription()
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Unregister Action...");
            System.Console.WriteLine();
            System.Console.WriteLine("This action removes repackinator's context menu (needs admin privileges).");
            System.Console.WriteLine();
            GetOptions().WriteOptionDescriptions(System.Console.Out);
        }

        public static void Process(string version, string[] args)
        {
            try
            {
                var options = GetOptions();
                options.Parse(args);
                if (ShowHelp)
                {
                    ConsoleUtil.ShowHelpHeaderForAction(version, Action, options);
                    ConsoleUtil.ProcessWait(Wait);
                    return;
                }

                if (!OperatingSystem.IsWindows())
                {
                    System.Console.WriteLine("Unregister action is only available on Windows.");
                    Environment.ExitCode = 1;
                    ConsoleUtil.ProcessWait(Wait);
                    return;
                }

                if (!Utility.IsAdmin())
                {
                    System.Console.WriteLine("Error: This action requires administrator privileges.");
                    System.Console.WriteLine("Please run this command as administrator (right-click and select 'Run as administrator').");
                    Environment.ExitCode = 1;
                    ConsoleUtil.ProcessWait(Wait);
                    return;
                }

                var result = ContextMenu.UnregisterContext();
                if (result)
                {
                    System.Console.WriteLine("Context menu removed.");
                }
                else
                {
                    System.Console.WriteLine("Failed to remove context menu.");
                    Environment.ExitCode = 1;
                }
            }
            catch (OptionException e)
            {
                ConsoleUtil.ShowOptionException(e);
                Environment.ExitCode = 1;
            }

            ConsoleUtil.ProcessWait(Wait);
        }
    }
}


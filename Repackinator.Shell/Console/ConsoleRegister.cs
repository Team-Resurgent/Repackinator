using Mono.Options;
using Repackinator.Shell.Shell;
using Repackinator.Core.Helpers;

namespace Repackinator.Shell.Console
{
    public static class ConsoleRegister
    {
        public const string Action = "Register";
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
            System.Console.WriteLine("Register Action...");
            System.Console.WriteLine();
            System.Console.WriteLine("This action adds or updates repackinator's context menu (needs admin privileges).");
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
                    System.Console.WriteLine("Register action is only available on Windows.");
                    Environment.ExitCode = 1;
                    ConsoleUtil.ProcessWait(Wait);
                    return;
                }

                if (!Utility.IsAdmin())
                {
                    System.Console.WriteLine("This action requires administrator privileges.");
                    System.Console.WriteLine("Attempting to elevate permissions...");
                    
                    // Reconstruct original arguments for elevation
                    var elevatedArgs = new List<string> { "-a=register" };
                    if (Wait)
                    {
                        elevatedArgs.Add("-w");
                    }
                    
                    if (Utility.RestartAsAdmin(elevatedArgs.ToArray()))
                    {
                        // Successfully started elevated process, exit this one
                        return;
                    }
                    else
                    {
                        System.Console.WriteLine("Error: Failed to elevate permissions. Please run this command as administrator.");
                        Environment.ExitCode = 1;
                        ConsoleUtil.ProcessWait(Wait);
                        return;
                    }
                }

                var result = ContextMenu.RegisterContext();
                if (result)
                {
                    System.Console.WriteLine("Context menu added.");
                }
                else
                {
                    System.Console.WriteLine("Failed to add context menu.");
                    Environment.ExitCode = 1;
                }
            }
            catch (OptionException e)
            {
                ConsoleUtil.ShowOptionException(e, Action, version);
                Environment.ExitCode = 1;
            }

            ConsoleUtil.ProcessWait(Wait);
        }
    }
}


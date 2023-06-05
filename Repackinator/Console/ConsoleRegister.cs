﻿using Mono.Options;
using Repackinator.Shell;

namespace Repackinator.Console
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

                var result = ContextMenu.RegisterContext();
                if (result)
                {
                    System.Console.WriteLine("Context menu added.");
                }
                else
                {
                    System.Console.WriteLine("Failed to add context menu (need to run as admin).");
                }
            }
            catch (OptionException e)
            {
                ConsoleUtil.ShowOptionException(e);
            }
        }
    }
}

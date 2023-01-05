﻿using Mono.Options;
using Repackinator.Helpers;
using Resurgent.UtilityBelt.Library.Utilities.ImageInput;
using Resurgent.UtilityBelt.Library.Utilities;
using Repackinator.Models;
using Repackinator.Shell;

namespace Repackinator.Console
{
    public static class ConsoleRegister
    {
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
            var options = ConsoleConvert.GetOptions();
            options.WriteOptionDescriptions(System.Console.Out);
        }

        public static void Process(string version, string[] args)
        {
            try
            {
                var options = GetOptions();
                options.Parse(args);
                if (ShowHelp)
                {
                    ConsoleUtil.ShowHelpHeader(version);
                    options.WriteOptionDescriptions(System.Console.Out);
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

            ConsoleUtil.ProcessWait(Wait);
        }
    }
}

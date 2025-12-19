using Mono.Options;

namespace Repackinator.Shell.Console
{
    public static class ConsoleStartup
    {
        public static string Action { get; set; } = string.Empty;
        public static bool ShowHelp { get; set; } = false;
        public static bool Wait { get; set; } = false;

        private static Dictionary<string, Action<string, string[]>> ActionsRegister = BuildActionsRegister();

        private static Dictionary<string, Action<string, string[]>> BuildActionsRegister()
        {
            var dict = new Dictionary<string, Action<string, string[]>>(StringComparer.CurrentCultureIgnoreCase);
            
            if (OperatingSystem.IsWindows())
            {
                dict.Add(ConsoleRegister.Action, ConsoleRegister.Process);
                dict.Add(ConsoleUnregister.Action, ConsoleUnregister.Process);
            }
            
            dict.Add(ConsoleConvert.Action, ConsoleConvert.Process);
            dict.Add(ConsoleCompare.Action, ConsoleCompare.Process);
            dict.Add(ConsoleInfo.Action, ConsoleInfo.Process);
            dict.Add(ConsoleChecksum.Action, ConsoleChecksum.Process);
            dict.Add(ConsoleExtract.Action, ConsoleExtract.Process);
            dict.Add(ConsoleRepack.Action, ConsoleRepack.Process);
            dict.Add(ConsolePack.Action, ConsolePack.Process);
            
            return dict;
        }

        public static void Process(string version, string[] args)
        {
            var options = new OptionSet {
                { "a|action=", $"Action ({string.Join(", ", ActionsRegister.Keys)})", a => Action = a },
                { "h|help", "show help", h => ShowHelp = h != null },
                { "w|wait", "Wait on exit", w => Wait = w != null }
            };

            try
            {
                options.Parse(args);
                if (ShowHelp && string.IsNullOrEmpty(Action))
                {
                    // Show concise main help with action summaries
                    var actionDescriptions = new Dictionary<string, string>
                    {
                        { "Pack", "Pack a folder into ISO or CCI format" },
                        { "Repack", "Repackinate a collection of Xbox disk images" },
                        { "Convert", "Convert one Xbox disk image format to another" },
                        { "Extract", "Extract files from Xbox disk image" },
                        { "Compare", "Compare two Xbox disk images" },
                        { "Info", "Show Xbox disk data sector information" },
                        { "Checksum", "Calculate checksum of Xbox disk image sectors" }
                    };

                    if (OperatingSystem.IsWindows())
                    {
                        actionDescriptions.Add("Register", "Register context menu (Windows, requires admin)");
                        actionDescriptions.Add("Unregister", "Unregister context menu (Windows, requires admin)");
                    }

                    ConsoleUtil.ShowHelpHeader(version, options, actionDescriptions);
                    ConsoleUtil.ProcessWait(Wait);
                    return;
                }

                if (string.IsNullOrEmpty(Action))
                {
                    // Show main help when no action is specified
                    var actionDescriptions = new Dictionary<string, string>
                    {
                        { "Pack", "Pack a folder into ISO or CCI format" },
                        { "Repack", "Repackinate a collection of Xbox disk images" },
                        { "Convert", "Convert one Xbox disk image format to another" },
                        { "Extract", "Extract files from Xbox disk image" },
                        { "Compare", "Compare two Xbox disk images" },
                        { "Info", "Show Xbox disk data sector information" },
                        { "Checksum", "Calculate checksum of Xbox disk image sectors" }
                    };

                    if (OperatingSystem.IsWindows())
                    {
                        actionDescriptions.Add("Register", "Register context menu (Windows, requires admin)");
                        actionDescriptions.Add("Unregister", "Unregister context menu (Windows, requires admin)");
                    }

                    ConsoleUtil.ShowHelpHeader(version, options, actionDescriptions);
                    ConsoleUtil.ProcessWait(Wait);
                    return;
                }

                if (ActionsRegister.ContainsKey(Action))
                {
                    ActionsRegister[Action](version, args);
                }
                else
                {
                    throw new OptionException($"Action '{Action}' is not valid. Use -h to see available actions.", "action");
                }
            }
            catch (OptionException e)
            {
                ConsoleUtil.ShowOptionException(e, Action, version);
            }

            ConsoleUtil.ProcessWait(Wait);
        }
    }
}


using Mono.Options;

namespace Repackinator.Core.Console
{
    public static class ConsoleStartup
    {
        public static string Action { get; set; } = string.Empty;
        public static bool ShowHelp { get; set; } = false;
        public static bool Wait { get; set; } = false;

        private static Dictionary<string, Action<string, string[]>> ActionsRegister = new Dictionary<string, Action<string, string[]>>(StringComparer.CurrentCultureIgnoreCase)
        {
            { ConsoleRegister.Action, ConsoleRegister.Process, OperatingSystem.IsWindows() }, // Conditional on windows entry
            { ConsoleUnregister.Action, ConsoleUnregister.Process, OperatingSystem.IsWindows() }, // Conditional on windows entry
            { ConsoleConvert.Action, ConsoleConvert.Process },
            { ConsoleCompare.Action, ConsoleCompare.Process },
            { ConsoleInfo.Action, ConsoleInfo.Process },
            { ConsoleChecksum.Action, ConsoleChecksum.Process },
            { ConsoleExtract.Action, ConsoleExtract.Process },
            { ConsoleRepack.Action, ConsoleRepack.Process },
        };

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
                if (ShowHelp && args.Length == 1)
                {
                    ConsoleUtil.ShowHelpHeader(version, options);

                    if (OperatingSystem.IsWindows())
                    {
                        ConsoleRegister.ShowOptionDescription();
                        ConsoleUnregister.ShowOptionDescription();
                    }

                    ConsoleConvert.ShowOptionDescription();
                    ConsoleCompare.ShowOptionDescription();
                    ConsoleInfo.ShowOptionDescription();
                    ConsoleChecksum.ShowOptionDescription();
                    ConsoleExtract.ShowOptionDescription();
                    ConsoleRepack.ShowOptionDescription();

                    ConsoleUtil.ProcessWait(Wait);
                    return;
                }

                if (ActionsRegister.ContainsKey(Action))
                {
                    ActionsRegister[Action](version, args);
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

        // Custom extension to dictionary to allow for conditional entries
        public static void Add(this Dictionary<string, Action<string, string[]>> dict, string key, Action<string, string[]> value, bool condition)
        {
            if (condition)
            {
                dict.Add(key, value);
            }
        }

    }
}

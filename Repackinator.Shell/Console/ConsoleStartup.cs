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
                    ConsolePack.ShowOptionDescription();

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
    }
}


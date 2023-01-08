using Mono.Options;
using Repackinator.Shell;

namespace Repackinator.Console
{
    public static class ConsoleStartup
    {
        public static string Action { get; set; } = string.Empty;
        public static bool ShowHelp { get; set; } = false;
        public static bool Wait { get; set; } = false;

        public static void Process(string version, string[] args)
        {
            var actions = $"{(OperatingSystem.IsWindows() ? $"{ConsoleRegister.Action}, {ConsoleUnregister.Action}, " : "" )}{ConsoleConvert.Action}, {ConsoleCompare.Action}, {ConsoleInfo.Action}, {ConsoleChecksum.Action}, {ConsoleExtract.Action}, {ConsoleRepack.Action}";

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

                if (OperatingSystem.IsWindows() && Action.Equals(ConsoleRegister.Action, StringComparison.CurrentCultureIgnoreCase))
                {
                    ConsoleRegister.Process(version, args);
                }
                else if (OperatingSystem.IsWindows() && Action.Equals(ConsoleUnregister.Action, StringComparison.CurrentCultureIgnoreCase))
                {
                    ConsoleUnregister.Process(version, args);
                }
                else if (Action.Equals(ConsoleConvert.Action, StringComparison.CurrentCultureIgnoreCase))
                {
                    ConsoleConvert.Process(version, args);
                }
                else if (Action.Equals(ConsoleCompare.Action, StringComparison.CurrentCultureIgnoreCase))
                {
                    ConsoleCompare.Process(version, args);
                }
                else if (Action.Equals(ConsoleInfo.Action, StringComparison.CurrentCultureIgnoreCase))
                {
                    ConsoleInfo.Process(version, args);
                }
                else if (Action.Equals(ConsoleChecksum.Action, StringComparison.CurrentCultureIgnoreCase))
                {
                    ConsoleChecksum.Process(version, args);
                }
                else if (Action.Equals(ConsoleExtract.Action, StringComparison.CurrentCultureIgnoreCase))
                {
                    ConsoleExtract.Process(version, args);
                }
                else if (Action.Equals(ConsoleRepack.Action, StringComparison.CurrentCultureIgnoreCase))
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

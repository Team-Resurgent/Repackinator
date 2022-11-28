using Mono.Options;
using Repackinator.Shared;
using Resurgent.UtilityBelt.Library.Utilities;
using Resurgent.UtilityBelt.Library.Utilities.ImageInput;
using System;
using System.Diagnostics;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace Repackinator
{
    public static class ConsoleStartup
    {
        private static string ActionConvert = "Convert";
        private static string ActionCompare = "Compare";

        private static string ScrubModeNone = "None";
        private static string ScrubModeScrub = "Scrub";
        private static string ScrubModeTruncate = "Truncate";

        private static void ProcessConvert(string version, bool shouldShowHelp, string[] args)
        {
            var input = string.Empty;
            var scrubMode = "NONE";
            var compress = false;
            var wait = false;

            try
            {
                var convertOptions = new OptionSet {
                    { "i|input=", "Input file", i => input = i },
                    { "s|scrub=", "Scrub mode (None *default*, Scub, Truncate)", s => scrubMode = s },
                    { "c|compress", "Compress", c => compress = c != null },
                    { "w|wait", "Wait on exit", w => wait = w != null }
                };
                convertOptions.Parse(args);
                if (shouldShowHelp && args.Length == 2)
                {
                    Console.WriteLine($"Repackinator {version}");
                    Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
                    Console.WriteLine("Credits go to HoRnEyDvL, Hazeno, Rocky5, Team Cerbios.");
                    Console.WriteLine();
                    Console.WriteLine("Usage: Repackinator [options]+");
                    Console.WriteLine();
                    convertOptions.WriteOptionDescriptions(Console.Out);
                    return;
                }

                input = Path.GetFullPath(input);
                if (!File.Exists(input))
                {
                    throw new OptionException("Input is not a valid file.", "input");
                }

                if (!string.Equals(scrubMode, ScrubModeNone, StringComparison.CurrentCultureIgnoreCase) &&
                    !string.Equals(scrubMode, ScrubModeScrub, StringComparison.CurrentCultureIgnoreCase) &&
                    !string.Equals(scrubMode, ScrubModeTruncate, StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new OptionException("Scrub mode is not valid.", "scrub");
                }
            }
            catch (OptionException e)
            {
                Console.Write("Repackinator by EqUiNoX: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `Repackinator --help' for more information.");
            }

            if (wait)
            {
                Console.Write("Press any key to continue.");
                Console.Read();
            }
        }

        private static void ProcessCompare(string version, bool shouldShowHelp, string[] args)
        {
            var config = Config.LoadConfig();

            var first = string.Empty;
            var second = string.Empty;
            var compare = false;
            var wait = false;

            try
            {
                var compareOptions = new OptionSet {
                    { "f|first=", "Set first file to compare", f => first = f },
                    { "s|second=", "Set second file to compare", s => second = s },
                    { "c|compare", "Compare", c => compare = c != null },
                    { "w|wait", "Wait on exit", w => wait = w != null }
                };
                compareOptions.Parse(args);
                if (shouldShowHelp && args.Length == 2)
                {
                    Console.WriteLine($"Repackinator {version}");
                    Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
                    Console.WriteLine("Credits go to HoRnEyDvL, Hazeno, Rocky5, Team Cerbios.");
                    Console.WriteLine();
                    Console.WriteLine("Usage: Repackinator [options]+");
                    Console.WriteLine();
                    compareOptions.WriteOptionDescriptions(Console.Out);
                    return;
                }

                if (!string.IsNullOrEmpty(first))
                {
                    if (!File.Exists(first))
                    {
                        throw new OptionException("First is not a valid file.", "first");
                    }
                    config.CompareFirst = Path.GetFullPath(first);
                    Config.SaveConfig(config);
                }

                if (!string.IsNullOrEmpty(second))
                {
                    if (!File.Exists(second))
                    {
                        throw new OptionException("Second is not a valid file.", "second");
                    }
                    config.CompareSecond = Path.GetFullPath(second);
                    Config.SaveConfig(config);
                }

                if (compare || (!string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(second)))
                {
                    if (!File.Exists(config.CompareFirst))
                    {
                        throw new OptionException("First is not a valid file.", "input");
                    }

                    if (!File.Exists(config.CompareSecond))
                    {
                        throw new OptionException("Second is not a valid file.", "input");
                    }

                    Console.WriteLine("Comparing:");
                    var firstSlices = Utility.GetSlicesFromFile(config.CompareFirst);
                    foreach (var firstSlice in firstSlices)
                    {
                        Console.WriteLine(Path.GetFileName(firstSlice));
                    }

                    Console.WriteLine("Against:");
                    var secondSlices = Utility.GetSlicesFromFile(config.CompareSecond);
                    foreach (var secondSlice in secondSlices)
                    {
                        Console.WriteLine(Path.GetFileName(secondSlice));
                    }

                    Console.WriteLine();

                    Console.WriteLine("Processing...");
                    XisoUtility.CompareXISO(ImageImputHelper.GetImageInput(firstSlices), ImageImputHelper.GetImageInput(secondSlices), s => {
                        Console.WriteLine(s);
                    });

                    Console.WriteLine();
                    Console.WriteLine("Compare complted.");
                }
            }
            catch (OptionException e)
            {
                Console.Write("Repackinator by EqUiNoX: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `Repackinator --help' for more information.");
            }

            if (wait)
            {
                Console.Write("Press any key to continue.");
                Console.Read();
            }
        }

        public static void Start(string version, string[] args)
        {
            var shouldShowHelp = false;
            var action = string.Empty;
            
            //var input = "";
            //var output = "";
            //var grouping = "NONE";
            //var log = "";

            //truncate

            var mainOptions = new OptionSet {
                { "a|action=", "Action (Convert, Compare)", a => action = a },
                { "h|help", "Show this help or for provided action", h => shouldShowHelp = true },
            };

            try
            {
                mainOptions.Parse(args);
                if (shouldShowHelp && args.Length == 1)
                {
                    Console.WriteLine($"Repackinator {version}");
                    Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
                    Console.WriteLine("Credits go to HoRnEyDvL, Hazeno, Rocky5, Team Cerbios.");
                    Console.WriteLine();
                    Console.WriteLine("Usage: Repackinator [options]+");
                    Console.WriteLine();
                    mainOptions.WriteOptionDescriptions(Console.Out);
                    return;
                }
                if (action.Equals(ActionConvert, StringComparison.CurrentCultureIgnoreCase))
                {
                    ProcessConvert(version, shouldShowHelp, args);
                }
                else if (action.Equals(ActionCompare, StringComparison.CurrentCultureIgnoreCase))
                {
                    ProcessCompare(version, shouldShowHelp, args);
                }
                else
                {
                    throw new OptionException("Action is not valid.", "action");
                }
            }
            catch (OptionException e)
            {
                Console.Write("Repackinator by EqUiNoX: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `Repackinator --help' for more information.");
            }

            //var optionset = new OptionSet {
            //    { "i|input=", "Input folder", i => input = i },
            //    { "o|output=", "Output folder", o => output = o },
            //    { "g|grouping=", "Grouping (None *default*, Region, Letter, RegionLetter, LetterRegion)", g => grouping = g.ToUpper() },
            //    { "l|log=", "log file", l => log = l },
            //    { "h|help", "show this message and exit", h => shouldShowHelp = h != null },
            //};

            //try
            //{
            //    optionset.Parse(args);
            //    if (shouldShowHelp || args.Length == 0)
            //    {
            //        Console.WriteLine("Usage: Repackinator");
            //        Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
            //        Console.WriteLine("Credits go to HoRnEyDvL, Hazeno, Rocky5, Team Cerbios.");
            //        Console.WriteLine();
            //        Console.WriteLine("Usage: Repackinator [options]+");
            //        Console.WriteLine();
            //        optionset.WriteOptionDescriptions(Console.Out);
            //        return;
            //    }

            //    if (string.IsNullOrEmpty(input))
            //    {
            //        throw new OptionException("input not specified.", "input");
            //    }

            //    input = Path.GetFullPath(input);
            //    if (!Directory.Exists(input))
            //    {
            //        throw new OptionException("input is not a valid directory.", "input");
            //    }

            //    if (string.IsNullOrEmpty(output))
            //    {
            //        throw new OptionException("output not specified.", "output");
            //    }

            //    if (!string.Equals(grouping, "NONE") && !string.Equals(grouping, "REGION") && !string.Equals(grouping, "LETTER") && !string.Equals(grouping, "REGIONLETTER") && !string.Equals(grouping, "LETTERREGION"))
            //    {
            //        throw new OptionException("grouping is not valid.", "grouping");
            //    }

            //    output = Path.GetFullPath(output);
            //    if (!Directory.Exists(output))
            //    {
            //        Directory.CreateDirectory(output);
            //    }

            //    if (!string.IsNullOrEmpty(log))
            //    {
            //        log = Path.GetFullPath(log);
            //    }

            //    var groupingValue = GroupingEnum.None;

            //    if (string.Equals(grouping, "REGION"))
            //    {
            //        groupingValue = GroupingEnum.Region;
            //    }
            //    else if (string.Equals(grouping, "LETTER"))
            //    {
            //        groupingValue = GroupingEnum.Letter;
            //    }
            //    else if (string.Equals(grouping, "REGIONLETTER"))
            //    {
            //        groupingValue = GroupingEnum.RegionLetter;
            //    }
            //    else if (string.Equals(grouping, "LETTERREGION"))
            //    {
            //        groupingValue = GroupingEnum.LetterRegion;
            //    }

            //    FileStream? logStream = null;
            //    if (!string.IsNullOrEmpty(log))
            //    {
            //        logStream = File.OpenWrite(log);
            //    }

            //    var logger = new Action<LogMessage>((logMessage) =>
            //    {
            //        var formattedTime = logMessage.Time.ToString("HH:mm:ss");
            //        var message = $"{formattedTime} {logMessage.Level} - {logMessage.Message}";
            //        Console.WriteLine(message);
            //        var bytes = Encoding.UTF8.GetBytes(message);
            //        if (logStream == null)
            //        {
            //            return;
            //        }
            //        logStream.Write(bytes);
            //    });

            //    var config = new Config
            //    {
            //        InputPath = input,
            //        OutputPath = output,
            //        Grouping = groupingValue
            //    };

            //    var cancellationTokenSource = new CancellationTokenSource();

            //    var gameData = GameDataHelper.LoadGameData();

            //    var repacker = new Repacker();
            //    repacker.StartRepacking(gameData, config, null, logger, new Stopwatch(), cancellationTokenSource.Token);

            //    if (logStream != null)
            //    {
            //        logStream.Dispose();
            //    }

            //    Console.WriteLine("Done!");
            //    Console.ReadLine();


        }
    }
}

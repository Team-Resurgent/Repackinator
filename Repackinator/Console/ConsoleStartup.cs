using Mono.Options;
using Repackinator.Helpers;
using Repackinator.Models;
using Resurgent.UtilityBelt.Library.Utilities;
using Resurgent.UtilityBelt.Library.Utilities.ImageInput;
using System;
using System.Diagnostics;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace Repackinator.Console
{
    public static class ConsoleStartup
    {
        private static string ActionConvert = "Convert";
        private static string ActionCompare = "Compare";

        private static string ScrubModeNone = "None";
        private static string ScrubModeScrub = "Scrub";
        private static string ScrubModeTrimmedScrub = "TrimmedScrub";

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
                    { "s|scrub=", "Scrub mode (None *default*, Scub, TrimmedScrub)", s => scrubMode = s },
                    { "c|compress", "Compress", c => compress = c != null },
                    { "w|wait", "Wait on exit", w => wait = w != null }
                };
                convertOptions.Parse(args);
                if (shouldShowHelp && args.Length == 2)
                {
                    System.Console.WriteLine($"Repackinator {version}");
                    System.Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
                    System.Console.WriteLine("Credits go to HoRnEyDvL, Hazeno, Rocky5, Team Cerbios.");
                    System.Console.WriteLine();
                    System.Console.WriteLine("Usage: Repackinator [options]+");
                    System.Console.WriteLine();
                    convertOptions.WriteOptionDescriptions(System.Console.Out);
                    return;
                }

                input = Path.GetFullPath(input);
                if (!File.Exists(input))
                {
                    throw new OptionException("Input is not a valid file.", "input");
                }

                var outputPath = Path.GetDirectoryName(input);
                var outputNameWithoutExtension = Path.GetFileNameWithoutExtension(input);
                var subExtension = Path.GetExtension(outputNameWithoutExtension);
                if (subExtension.Equals(".1") || subExtension.Equals(".2"))
                {
                    outputNameWithoutExtension = Path.GetFileNameWithoutExtension(outputNameWithoutExtension);
                }

                bool scrub = false;
                bool trimmedScrub = false;

                if (string.Equals(scrubMode, ScrubModeScrub, StringComparison.CurrentCultureIgnoreCase))
                {
                    scrub = true;
                    outputNameWithoutExtension = $"{outputNameWithoutExtension}-Scrub";
                }
                else if (string.Equals(scrubMode, ScrubModeTrimmedScrub, StringComparison.CurrentCultureIgnoreCase))
                {
                    scrub = true;
                    trimmedScrub = true;
                    outputNameWithoutExtension = $"{outputNameWithoutExtension}-TrimmedScrub";
                }
                else if (!string.Equals(scrubMode, ScrubModeNone, StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new OptionException("Scrub mode is not valid.", "scrub");
                }

                System.Console.WriteLine("Converting:");
                var inputSlices = Utility.GetSlicesFromFile(input);
                foreach (var inputSlice in inputSlices)
                {
                    System.Console.WriteLine(Path.GetFileName(inputSlice));
                }

                if (outputPath != null)
                {
                    outputPath = Path.Combine(outputPath, "Converted");
                    Directory.CreateDirectory(outputPath);

                    if (compress)
                    {
                        XisoUtility.CreateCCI(ImageImputHelper.GetImageInput(inputSlices), outputPath, outputNameWithoutExtension, ".cci", scrub, trimmedScrub, p =>
                        {
                            System.Console.Write($"Progress {Math.Round(p * 100)}%");
                            System.Console.CursorLeft = 0;
                        }, default);
                    }
                    else
                    {
                        XisoUtility.Split(ImageImputHelper.GetImageInput(inputSlices), outputPath, outputNameWithoutExtension, ".iso", scrub, trimmedScrub, p =>
                        {
                            System.Console.Write($"Progress {Math.Round(p * 100)}%");
                            System.Console.CursorLeft = 0;
                        }, default);
                    }
                }

                System.Console.WriteLine();
                System.Console.WriteLine("Convert completed.");
            }
            catch (OptionException e)
            {
                System.Console.Write("Repackinator by EqUiNoX: ");
                System.Console.WriteLine(e.Message);
                System.Console.WriteLine("Try `Repackinator --help' for more information.");
            }

            if (wait)
            {
                System.Console.Write("Press any key to continue.");
                System.Console.ReadKey();
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
                    System.Console.WriteLine($"Repackinator {version}");
                    System.Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
                    System.Console.WriteLine("Credits go to HoRnEyDvL, Hazeno, Rocky5, Team Cerbios.");
                    System.Console.WriteLine();
                    System.Console.WriteLine("Usage: Repackinator [options]+");
                    System.Console.WriteLine();
                    compareOptions.WriteOptionDescriptions(System.Console.Out);
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

                if (compare || !string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(second))
                {
                    if (!File.Exists(config.CompareFirst))
                    {
                        throw new OptionException("First is not a valid file.", "input");
                    }

                    if (!File.Exists(config.CompareSecond))
                    {
                        throw new OptionException("Second is not a valid file.", "input");
                    }

                    System.Console.WriteLine("Comparing:");
                    var firstSlices = Utility.GetSlicesFromFile(config.CompareFirst);
                    foreach (var firstSlice in firstSlices)
                    {
                        System.Console.WriteLine(Path.GetFileName(firstSlice));
                    }

                    System.Console.WriteLine("Against:");
                    var secondSlices = Utility.GetSlicesFromFile(config.CompareSecond);
                    foreach (var secondSlice in secondSlices)
                    {
                        System.Console.WriteLine(Path.GetFileName(secondSlice));
                    }

                    System.Console.WriteLine();

                    System.Console.WriteLine("Processing...");
                    XisoUtility.CompareXISO(ImageImputHelper.GetImageInput(firstSlices), ImageImputHelper.GetImageInput(secondSlices), s =>
                    {
                        System.Console.WriteLine(s);
                    }, p =>
                    {
                        System.Console.Write($"Progress {Math.Round(p * 100)}%");
                        System.Console.CursorLeft = 0;
                    });

                    System.Console.WriteLine();
                    System.Console.WriteLine("Compare completed.");
                }
            }
            catch (OptionException e)
            {
                System.Console.Write("Repackinator by EqUiNoX: ");
                System.Console.WriteLine(e.Message);
                System.Console.WriteLine("Try `Repackinator --help' for more information.");
            }

            if (wait)
            {
                System.Console.Write("Press any key to continue.");
                System.Console.Read();
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

            //trimmedScrub

            var mainOptions = new OptionSet {
                { "a|action=", "Action (Convert, Compare)", a => action = a },
                { "h|help", "Show this help or for provided action", h => shouldShowHelp = true },
            };

            try
            {
                mainOptions.Parse(args);
                if (shouldShowHelp && args.Length == 1)
                {
                    System.Console.WriteLine($"Repackinator {version}");
                    System.Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
                    System.Console.WriteLine("Credits go to HoRnEyDvL, Hazeno, Rocky5, Team Cerbios.");
                    System.Console.WriteLine();
                    System.Console.WriteLine("Usage: Repackinator [options]+");
                    System.Console.WriteLine();
                    mainOptions.WriteOptionDescriptions(System.Console.Out);
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
                System.Console.Write("Repackinator by EqUiNoX: ");
                System.Console.WriteLine(e.Message);
                System.Console.WriteLine("Try `Repackinator --help' for more information.");
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

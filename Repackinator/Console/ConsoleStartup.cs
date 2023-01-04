using Mono.Options;
using Repackinator.Actions;
using Repackinator.Helpers;
using Repackinator.Logging;
using Repackinator.Models;
using Resurgent.UtilityBelt.Library.Utilities;
using Resurgent.UtilityBelt.Library.Utilities.ImageInput;
using System.Diagnostics;
using System.Text;

namespace Repackinator.Console
{
    public static class ConsoleStartup
    {
        private static string ActionConvert = "Convert";
        private static string ActionCompare = "Compare";
        private static string ActionInfo = "Info";
        private static string ActionChecksum = "Checksum";
        private static string ActionExtract = "Extract";
        private static string ActionRepack = "Repack";

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
                    { "s|scrub=", "Scrub mode (None *default*, Scrub, TrimmedScrub)", s => scrubMode = s },
                    { "c|compress", "Compress", c => compress = c != null },
                    { "w|wait", "Wait on exit", w => wait = w != null }
                };
                convertOptions.Parse(args);
                if (shouldShowHelp)
                {
                    System.Console.WriteLine($"Repackinator {version}");
                    System.Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
                    System.Console.WriteLine("Credits go to HoRnEyDvL, Hazeno, Rocky5, navi, Fredr1kh, Natetronn, Incursion64, Zatchbot, Team Cerbios.");
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

                var previousProgress = -1.0f;

                if (outputPath != null)
                {
                    outputPath = Path.Combine(outputPath, "Converted");
                    Directory.CreateDirectory(outputPath);

                    if (compress)
                    {
                        XisoUtility.CreateCCI(ImageImputHelper.GetImageInput(inputSlices), outputPath, outputNameWithoutExtension, ".cci", scrub, trimmedScrub, (s, p) =>
                        {
                            var amount = (float)Math.Round(p * 100);
                            if (amount != previousProgress)
                            {
                                System.Console.Write($"Stage {s + 1} of 3, Progress {amount}%");
                                System.Console.CursorLeft = 0;
                                previousProgress = amount;
                            }
                        }, default);
                    }
                    else
                    {
                        XisoUtility.Split(ImageImputHelper.GetImageInput(inputSlices), outputPath, outputNameWithoutExtension, ".iso", scrub, trimmedScrub, (s, p) =>
                        {
                            var amount = (float)Math.Round(p * 100);
                            if (amount != previousProgress)
                            {
                                System.Console.Write($"Stage {s + 1} of 3, Progress {amount}%");
                                System.Console.CursorLeft = 0;
                                previousProgress = amount;
                            }                            
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
                if (shouldShowHelp)
                {
                    System.Console.WriteLine($"Repackinator {version}");
                    System.Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
                    System.Console.WriteLine("Credits go to HoRnEyDvL, Hazeno, Rocky5, navi, Fredr1kh, Natetronn, Incursion64, Zatchbot, Team Cerbios.");
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

                    var previousProgress = -1.0f;
                    
                    System.Console.WriteLine("Processing...");
                    XisoUtility.CompareXISO(ImageImputHelper.GetImageInput(firstSlices), ImageImputHelper.GetImageInput(secondSlices), s =>
                    {
                        System.Console.WriteLine(s);
                    }, p =>
                    {
                        var amount = (float)Math.Round(p * 100);
                        if (amount != previousProgress) { 
                            System.Console.Write($"Progress {amount}%");
                            System.Console.CursorLeft = 0;
                            previousProgress = amount;
                        }
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

        private static void ProcessInfo(string version, bool shouldShowHelp, string[] args)
        {
            var config = Config.LoadConfig();

            var input = string.Empty;
            var wait = false;

            try
            {
                var compareOptions = new OptionSet {
                    { "i|input=", "Input file", i => input = i },
                    { "w|wait", "Wait on exit", w => wait = w != null }
                };
                compareOptions.Parse(args);
                if (shouldShowHelp)
                {
                    System.Console.WriteLine($"Repackinator {version}");
                    System.Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
                    System.Console.WriteLine("Credits go to HoRnEyDvL, Hazeno, Rocky5, navi, Fredr1kh, Natetronn, Incursion64, Zatchbot, Team Cerbios.");
                    System.Console.WriteLine();
                    System.Console.WriteLine("Usage: Repackinator [options]+");
                    System.Console.WriteLine();
                    compareOptions.WriteOptionDescriptions(System.Console.Out);
                    return;
                }

                if (!File.Exists(input))
                {
                    throw new OptionException("Input is not a valid file.", "input");
                }

                System.Console.WriteLine("Getting Info From:");               
                var inputSlices = Utility.GetSlicesFromFile(input);
                foreach (var inputSlice in inputSlices)
                {
                    System.Console.WriteLine(Path.GetFileName(inputSlice));
                }

                System.Console.WriteLine("Processing...");
                System.Console.WriteLine($"Type,Filename,Size,StartSector,EndSector,InSlices");
                XisoUtility.GetFileInfoFromXiso(ImageImputHelper.GetImageInput(inputSlices), f => {
                    var type = f.IsFile ? "F" : "D";
                    var startSector = f.StartSector > 0 ? f.StartSector.ToString() : "N/A";
                    var endSector = f.EndSector > 0 ? f.EndSector.ToString() : "N/A";
                    System.Console.WriteLine($"{type},{f.Filename},{f.Size},{startSector},{endSector},{f.InSlices}");
                }, null, default);

                System.Console.WriteLine();
                System.Console.WriteLine("Info completed.");
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

        private static void ProcessChecksum(string version, bool shouldShowHelp, string[] args)
        {
            var config = Config.LoadConfig();

            var input = string.Empty;
            var wait = false;

            try
            {
                var compareOptions = new OptionSet {
                    { "i|input=", "Input file", i => input = i },
                    { "w|wait", "Wait on exit", w => wait = w != null }
                };
                compareOptions.Parse(args);
                if (shouldShowHelp)
                {
                    System.Console.WriteLine($"Repackinator {version}");
                    System.Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
                    System.Console.WriteLine("Credits go to HoRnEyDvL, Hazeno, Rocky5, navi, Fredr1kh, Natetronn, Incursion64, Zatchbot, Team Cerbios.");
                    System.Console.WriteLine();
                    System.Console.WriteLine("Usage: Repackinator [options]+");
                    System.Console.WriteLine();
                    compareOptions.WriteOptionDescriptions(System.Console.Out);
                    return;
                }

                if (!File.Exists(input))
                {
                    throw new OptionException("Input is not a valid file.", "input");
                }

                System.Console.WriteLine("Calculating Checksum From:");
                var inputSlices = Utility.GetSlicesFromFile(input);
                foreach (var inputSlice in inputSlices)
                {
                    System.Console.WriteLine(Path.GetFileName(inputSlice));
                }

                var previousProgress = -1.0f;

                System.Console.WriteLine("Processing...");
                var result = XisoUtility.GetChecksumFromXiso(ImageImputHelper.GetImageInput(inputSlices), p =>
                {
                    var amount = (float)Math.Round(p * 100);
                    if (amount != previousProgress)
                    {
                        System.Console.Write($"Progress {amount}%");
                        System.Console.CursorLeft = 0;
                        previousProgress = amount;
                    }
                }, default);
                System.Console.WriteLine($"SHA256 = {result}");

                System.Console.WriteLine();
                System.Console.WriteLine("Checksum completed.");
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

        private static void ProcessExtract(string version, bool shouldShowHelp, string[] args)
        {
            var config = Config.LoadConfig();

            var input = string.Empty;
            var wait = false;

            try
            {
                var compareOptions = new OptionSet {
                    { "i|input=", "Input file", i => input = i },
                    { "w|wait", "Wait on exit", w => wait = w != null }
                };
                compareOptions.Parse(args);
                if (shouldShowHelp)
                {
                    System.Console.WriteLine($"Repackinator {version}");
                    System.Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
                    System.Console.WriteLine("Credits go to HoRnEyDvL, Hazeno, Rocky5, navi, Fredr1kh, Natetronn, Incursion64, Zatchbot, Team Cerbios.");
                    System.Console.WriteLine();
                    System.Console.WriteLine("Usage: Repackinator [options]+");
                    System.Console.WriteLine();
                    compareOptions.WriteOptionDescriptions(System.Console.Out);
                    return;
                }

                if (!File.Exists(input))
                {
                    throw new OptionException("Input is not a valid file.", "input");
                }

                System.Console.WriteLine("Calculating Checksum From:");
                var inputSlices = Utility.GetSlicesFromFile(input);
                foreach (var inputSlice in inputSlices)
                {
                    System.Console.WriteLine(Path.GetFileName(inputSlice));
                }

                System.Console.WriteLine("Extracting...");

                var outputPath = Path.GetDirectoryName(input);
                if (outputPath == null)
                {
                    throw new IOException("Unable to get directory name from input.");
                }
                outputPath = Path.Combine(outputPath, Utility.GetNameFromSlice(input));
                Directory.CreateDirectory(outputPath);

                var imageInput = ImageImputHelper.GetImageInput(inputSlices);

                var previousProgress = -1.0f;
                XisoUtility.GetFileInfoFromXiso(imageInput, f => {

                    if (!f.IsFile)
                    {
                        return;
                    }

                    var sector = f.StartSector;
                    var size = f.Size;
                    var result = new byte[size];
                    var processed = 0U;
                    if (size > 0)
                    {
                        while (processed < size)
                        {
                            var buffer = imageInput.ReadSectors(sector, 1);
                            var bytesToCopy = (uint)Math.Min(size - processed, 2048);
                            Array.Copy(buffer, 0, result, processed, bytesToCopy);
                            sector++;
                            processed += bytesToCopy;
                        }
                    }
                    var destPath = Path.Combine(outputPath, f.Path);
                    Directory.CreateDirectory(destPath);
                    var fileName = Path.Combine(destPath, f.Filename);
                    File.WriteAllBytes(fileName, result);

                },
                p =>
                {
                    var amount = (float)Math.Round(p * 100);
                    if (amount != previousProgress)
                    {
                        System.Console.Write($"Progress {amount}%");
                        System.Console.CursorLeft = 0;
                        previousProgress = amount;
                    }
                }, default);

                System.Console.WriteLine();
                System.Console.WriteLine("Extract completed.");
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

        private static void ProcessRepack(string version, bool shouldShowHelp, string[] args)
        {
            var input = string.Empty;
            var output = string.Empty;
            var grouping = "NONE";
            var upperCase = false;
            var recurse = false;
            var log = string.Empty;
            var compress = false;
            var trimmedScrub = false;
            var wait = false;

            try
            {
                var repackOptions = new OptionSet {
                    { "i|input=", "Input folder", i => input = i },
                    { "o|output=", "Output folder", o => output = o },
                    { "g|grouping=", "Grouping (None *default*, Region, Letter, RegionLetter, LetterRegion)", g => grouping = g.ToUpper() },
                    { "u|upperCase", "Upper Case", u => upperCase = u != null },
                    { "r|recurse", "Recurse (Traverse Sub Dirs)", r => recurse = r != null },
                    { "c|compress", "Compress", c => compress = c != null },
                    { "t|trimmedScrub", "Trimmed Scrub", t => trimmedScrub = t != null },
                    { "l|log=", "log file", l => log = l },
                    { "h|help", "show this message and exit", h => shouldShowHelp = h != null },
                    { "w|wait", "Wait on exit", w => wait = w != null }
                };
                repackOptions.Parse(args);
                if (shouldShowHelp)
                {
                    System.Console.WriteLine($"Repackinator {version}");
                    System.Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
                    System.Console.WriteLine("Credits go to HoRnEyDvL, Hazeno, Rocky5, navi, Fredr1kh, Natetronn, Incursion64, Zatchbot, Team Cerbios.");
                    System.Console.WriteLine();
                    System.Console.WriteLine("Usage: Repackinator [options]+");
                    System.Console.WriteLine();
                    repackOptions.WriteOptionDescriptions(System.Console.Out);
                    return;
                }

                if (string.IsNullOrEmpty(input))
                {
                    throw new OptionException("input not specified.", "input");
                }

                input = Path.GetFullPath(input);
                if (!Directory.Exists(input))
                {
                    throw new OptionException("input is not a valid directory.", "input");
                }

                if (string.IsNullOrEmpty(output))
                {
                    throw new OptionException("output not specified.", "output");
                }

                var groupingValue = GroupingEnum.None;
                if (string.Equals(grouping, "NONE"))
                {
                    groupingValue = GroupingEnum.None;
                }
                else if (string.Equals(grouping, "REGION"))
                {
                    groupingValue = GroupingEnum.Region;
                }
                else if (string.Equals(grouping, "LETTER"))
                {
                    groupingValue = GroupingEnum.Letter;
                }
                else if (string.Equals(grouping, "REGIONLETTER"))
                {
                    groupingValue = GroupingEnum.RegionLetter;
                }
                else if (string.Equals(grouping, "LETTERREGION"))
                {
                    groupingValue = GroupingEnum.LetterRegion;
                }
                else
                {
                    throw new OptionException("grouping is not valid.", "grouping");
                }

                output = Path.GetFullPath(output);
                if (!Directory.Exists(output))
                {
                    Directory.CreateDirectory(output);
                }

                if (!string.IsNullOrEmpty(log))
                {
                    log = Path.GetFullPath(log);
                }

                FileStream? logStream = null;
                if (!string.IsNullOrEmpty(log))
                {
                    logStream = File.OpenWrite(log);
                }

                var logger = new Action<LogMessage>((logMessage) =>
                {
                    var formattedTime = logMessage.Time.ToString("HH:mm:ss");
                    var message = $"{formattedTime} {logMessage.Level} - {logMessage.Message}";
                    System.Console.WriteLine(message);
                    var bytes = Encoding.UTF8.GetBytes(message);
                    if (logStream == null)
                    {
                        return;
                    }
                    logStream.Write(bytes);
                });

                var config = new Config
                {
                    InputPath = input,
                    OutputPath = output,
                    Grouping = groupingValue,
                    RecurseInput = recurse,
                    UpperCase = upperCase,
                    Compress = compress,
                    TrimmedScrub = trimmedScrub,
                };

                var gameData = GameDataHelper.LoadGameData();

                var prevMessage = string.Empty;

                var repacker = new Repacker();
                repacker.StartRepacking(gameData, config, null, logger, new Stopwatch(), default);

                if (logStream != null)
                {
                    logStream.Dispose();
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

        public static void Start(string version, string[] args)
        {
            var shouldShowHelp = false;
            var action = string.Empty;

            var mainOptions = new OptionSet {
                { "a|action=", "Action (Convert, Compare, Info, Checksum, Extract, Repack)", a => action = a },
                { "h|help", "Show this help or for provided action", h => shouldShowHelp = true },
            };

            try
            {
                mainOptions.Parse(args);
                if (shouldShowHelp && args.Length == 1)
                {
                    System.Console.WriteLine($"Repackinator {version}");
                    System.Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
                    System.Console.WriteLine("Credits go to HoRnEyDvL, Hazeno, Rocky5, navi, Fredr1kh, Natetronn, Incursion64, Zatchbot, Team Cerbios.");
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
                else if (action.Equals(ActionInfo, StringComparison.CurrentCultureIgnoreCase))
                {
                    ProcessInfo(version, shouldShowHelp, args);
                }
                else if (action.Equals(ActionChecksum, StringComparison.CurrentCultureIgnoreCase))
                {
                    ProcessChecksum(version, shouldShowHelp, args);
                }
                else if (action.Equals(ActionExtract, StringComparison.CurrentCultureIgnoreCase))
                {
                    ProcessExtract(version, shouldShowHelp, args);
                }
                else if (action.Equals(ActionRepack, StringComparison.CurrentCultureIgnoreCase))
                {
                    ProcessRepack(version, shouldShowHelp, args);
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
        }
    }
}

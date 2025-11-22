using Mono.Options;
using System.Text;
using Repackinator.Core.Actions;
using System.Diagnostics;
using Repackinator.Core.Models;
using Repackinator.Core.Logging;

namespace Repackinator.Core.Console
{
    public static class ConsoleRepack
    {
        public const string Action = "Repack";
        public static string Input { get; set; } = string.Empty;
        public static string Output { get; set; } = string.Empty;
        public static string Unpack { get; set; } = string.Empty;
        public static string Grouping { get; set; } = "NONE";
        public static bool UpperCase { get; set; } = false;
        public static bool Recurse { get; set; } = false;
        public static bool NoSplit { get; set; } = false;
        public static string Log { get; set; } = string.Empty;
        public static string CompressType { get; set; } = "NONE";
        public static string ScrubType { get; set; } = "NONE";
        public static bool ShowHelp { get; set; } = false;
        public static bool Wait { get; set; } = false;

        public static OptionSet GetOptions()
        {
            return new OptionSet {
                { "i|input=", "Input folder", i => Input = i },
                { "o|output=", "Output folder", o => Output = o },
                { "p|unpack=", "Unpack folder (optional)", p => Unpack = p},
                { "g|grouping=", "Grouping (None *default*, Region, Letter, RegionLetter, LetterRegion)", g => Grouping = g.ToUpper() },
                { "u|upperCase", "Upper Case", u => UpperCase = u != null },
                { "r|recurse", "Recurse (Traverse Sub Dirs)", r => Recurse = r != null },
                { "c|compress=", "Compress (None *default*, CCI)", c => CompressType = c.ToUpper() },
                { "s|scrub", "Scrub (None *default*, Scrub, TrimScrub)", s => ScrubType = s.ToUpper() },
                { "n|nosplit", "No Split of output file", n => NoSplit = n != null },
                { "l|log=", "log file", l => Log = l },
                { "h|help", "show help", h => ShowHelp = h != null },
                { "w|wait", "Wait on exit", w => Wait = w != null }
            };
        }

        public static void ShowOptionDescription()
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Repack Action...");
            System.Console.WriteLine();
            System.Console.WriteLine("This action is used to repackinate your collection of xbox disk images.");
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

                if (string.IsNullOrEmpty(Input))
                {
                    throw new OptionException("input not specified.", "input");
                }

                string input;
                try
                {
                    input = Path.GetFullPath(Input);
                }
                catch (ArgumentException)
                {
                    throw new OptionException("input is not a valid directory.", "input");
                }

                if (!Directory.Exists(input))
                {
                    throw new OptionException("input directory does not exist.", "input");
                }

                if (string.IsNullOrEmpty(Output))
                {
                    throw new OptionException("output not specified.", "output");
                }

                string output;
                try
                {
                    output = Path.GetFullPath(Output);
                }
                catch (ArgumentException)
                {
                    throw new OptionException("output is not a valid directory.", "output");
                }

                var groupingValue = GroupingOptionType.None;
                if (string.Equals(Grouping, "NONE"))
                {
                    groupingValue = GroupingOptionType.None;
                }
                else if (string.Equals(Grouping, "REGION"))
                {
                    groupingValue = GroupingOptionType.Region;
                }
                else if (string.Equals(Grouping, "LETTER"))
                {
                    groupingValue = GroupingOptionType.Letter;
                }
                else if (string.Equals(Grouping, "REGIONLETTER"))
                {
                    groupingValue = GroupingOptionType.RegionLetter;
                }
                else if (string.Equals(Grouping, "LETTERREGION"))
                {
                    groupingValue = GroupingOptionType.LetterRegion;
                }
                else
                {
                    throw new OptionException("grouping is not valid.", "grouping");
                }

                var compressValue = CompressOptionType.None;
                if (string.Equals(CompressType, "NONE"))
                {
                    compressValue = CompressOptionType.None;
                }
                else if (string.Equals(CompressType, "CCI"))
                {
                    compressValue = CompressOptionType.CCI;
                }
                else
                {
                    throw new OptionException("compress is not valid.", "compress");
                }

                var scrubValue = ScrubOptionType.None;
                if (string.Equals(ScrubType, "NONE"))
                {
                    scrubValue = ScrubOptionType.None;
                }
                else if (string.Equals(ScrubType, "SCRUB"))
                {
                    scrubValue = ScrubOptionType.Scrub;
                }
                else if (string.Equals(ScrubType, "TRIMSCRUB"))
                {
                    scrubValue = ScrubOptionType.TrimScrub;
                }

                string unpack;
                if (!string.IsNullOrEmpty(Unpack))
                {
                    try
                    {
                        unpack = Path.GetFullPath(Unpack);
                        if (!Directory.Exists(unpack))
                        {
                            Directory.CreateDirectory(unpack);
                        }
                    }
                    catch (ArgumentException)
                    {
                        throw new OptionException("unpack is not a valid directory.", "unpack");
                    }
                }

                if (!Directory.Exists(output))
                {
                    Directory.CreateDirectory(output);
                }

                string log;
                FileStream? logStream = null;
                if (!string.IsNullOrEmpty(Log))
                {
                    try
                    {
                        log = Path.GetFullPath(Log);
                        string? dir = Path.GetDirectoryName(log);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        logStream = File.OpenWrite(log);
                    }
                    catch (ArgumentException)
                    {
                        throw new OptionException("log is not a valid filepath.", "log");
                    }
                }

                var logger = new Action<LogMessage>((logMessage) =>
                {
                    var formattedTime = logMessage.Time.ToString("HH:mm:ss");
                    var message = $"{formattedTime} {logMessage.Level} - {logMessage.Message}";
                    System.Console.WriteLine(message);
                    logStream?.Write(Encoding.UTF8.GetBytes(message));
                });

                var config = new Config
                {
                    InputPath = Input,
                    OutputPath = Output,
                    UnpackPath = Unpack,
                    GroupingOption = groupingValue,
                    RecurseInput = Recurse,
                    Uppercase = UpperCase,
                    CompressOption = compressValue,
                    NoSplit = NoSplit,
                    ScrubOption = scrubValue,
                };

                var gameData = GameDataHelper.LoadGameData();

                var prevMessage = string.Empty;

                var repacker = new Repacker();
                repacker.StartRepacking(gameData, config, null, logger, new Stopwatch(), default);

                logStream?.Dispose();

                System.Console.WriteLine();
                System.Console.WriteLine("Convert completed.");
            }
            catch (OptionException e)
            {
                ConsoleUtil.ShowOptionException(e);
            }
        }
    }
}

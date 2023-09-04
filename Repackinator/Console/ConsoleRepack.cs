using Mono.Options;
using System.Text;
using Repackinator.Models;
using Repackinator.Actions;
using Repackinator.Logging;
using System.Diagnostics;

namespace Repackinator.Console
{
    public static class ConsoleRepack
    {
        public const string Action = "Repack";
        public static string Input { get; set; } = string.Empty;
        public static string Output { get; set; } = string.Empty;
        public static string Grouping { get; set; } = "NONE";
        public static bool UpperCase { get; set; } = false;
        public static bool Recurse { get; set; } = false;
        public static string Log { get; set; } = string.Empty;
        public static string CompressType { get; set; } = "NONE";
        public static bool TrimmedScrub { get; set; } = false;
        public static bool ShowHelp { get; set; } = false;
        public static bool Wait { get; set; } = false;

        public static OptionSet GetOptions()
        {
            return new OptionSet {
                { "i|input=", "Input folder", i => Input = i },
                { "o|output=", "Output folder", o => Output = o },
                { "g|grouping=", "Grouping (None *default*, Region, Letter, RegionLetter, LetterRegion)", g => Grouping = g.ToUpper() },
                { "u|upperCase", "Upper Case", u => UpperCase = u != null },
                { "r|recurse", "Recurse (Traverse Sub Dirs)", r => Recurse = r != null },
                { "c|compress", "Compress (None *default, CCi, CSO)", c => CompressType = c.ToUpper() },
                { "t|trimmedScrub", "Trimmed Scrub", t => TrimmedScrub = t != null },
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

                var input = Path.GetFullPath(Input);
                if (!Directory.Exists(input))
                {
                    throw new OptionException("input is not a valid directory.", "input");
                }

                if (string.IsNullOrEmpty(Output))
                {
                    throw new OptionException("output not specified.", "output");
                }

                var groupingValue = GroupingEnum.None;
                if (string.Equals(Grouping, "NONE"))
                {
                    groupingValue = GroupingEnum.None;
                }
                else if (string.Equals(Grouping, "REGION"))
                {
                    groupingValue = GroupingEnum.Region;
                }
                else if (string.Equals(Grouping, "LETTER"))
                {
                    groupingValue = GroupingEnum.Letter;
                }
                else if (string.Equals(Grouping, "REGIONLETTER"))
                {
                    groupingValue = GroupingEnum.RegionLetter;
                }
                else if (string.Equals(Grouping, "LETTERREGION"))
                {
                    groupingValue = GroupingEnum.LetterRegion;
                }
                else
                {
                    throw new OptionException("grouping is not valid.", "grouping");
                }

                var compressValue = CompressEnum.None;
                if (string.Equals(CompressType, "NONE"))
                {
                    compressValue = CompressEnum.None;
                }
                else if (string.Equals(CompressType, "CCI"))
                {
                    compressValue = CompressEnum.CCI;
                }
                else if (string.Equals(CompressType, "CSO"))
                {
                    compressValue = CompressEnum.CSO;
                }
                else
                {
                    throw new OptionException("compress is not valid.", "compress");
                }

                var output = Path.GetFullPath(Output);
                if (!Directory.Exists(output))
                {
                    Directory.CreateDirectory(output);
                }

                var log = Log;
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
                    InputPath = Input,
                    OutputPath = Output,
                    Grouping = groupingValue,
                    RecurseInput = Recurse,
                    UpperCase = UpperCase,
                    CompressType = compressValue,
                    TrimmedScrub = TrimmedScrub,
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
                ConsoleUtil.ShowOptionException(e);
            }
        }
    }
}

using Mono.Options;
using Repackinator.Core.Helpers;
using Resurgent.UtilityBelt.Library.Utilities.ImageInput;
using Resurgent.UtilityBelt.Library.Utilities;
using Repackinator.Core.Models;

namespace Repackinator.Core.Console
{
    public static class ConsoleConvert
    {
        public const string Action = "Convert";
        public static string Input { get; set; } = string.Empty;
        public static string ScrubMode { get; set; } = "NONE";
        public static string CompressType { get; set; } = "NONE";
        public static bool NoSplit { get; set; } = false;
        public static bool ShowHelp { get; set; } = false;
        public static bool Wait { get; set; } = false;
        public static bool Quiet { get; set; } = false;

        private static string ScrubModeNone = "None";
        private static string ScrubModeScrub = "Scrub";
        private static string ScrubModeTrimmedScrub = "TrimmedScrub";

        public static OptionSet GetOptions()
        {
            return new OptionSet {
                { "i|input=", "Input file", i => Input = i },
                { "s|scrub=", "Scrub mode (None *default*, Scrub, TrimmedScrub)", s => ScrubMode = s },
                { "c|compress=", "Compress (None *default*, CCI, CSO)", c => CompressType = c },
                { "n|nosplit", "No Split of output file", n => NoSplit = n != null },
                { "h|help", "show help", h => ShowHelp = h != null },
                { "w|wait", "Wait on exit", w => Wait = w != null },
                { "q|quiet", "Suppress status output", q => Quiet = q != null }
            };
        }

        public static void ShowOptionDescription()
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Convert Action...");
            System.Console.WriteLine();
            System.Console.WriteLine("This action is used to convert one xbox disk image format to another.");
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

                var input = Path.GetFullPath(Input);
                if (!File.Exists(input))
                {
                    throw new OptionException("Input is not a valid file.", "input");
                }

                var outputSuffix = string.Empty;
                var outputPath = Path.GetDirectoryName(input);
                if (outputPath == null)
                {
                    throw new OptionException("Unable to get directory from input.", "input");
                }

                var outputNameWithoutExtension = Path.GetFileNameWithoutExtension(input);
                var subExtension = Path.GetExtension(outputNameWithoutExtension);
                if (subExtension.Equals(".1") || subExtension.Equals(".2"))
                {
                    outputNameWithoutExtension = Path.GetFileNameWithoutExtension(outputNameWithoutExtension);
                }

                bool scrub = false;
                bool trimmedScrub = false;

                if (string.Equals(ScrubMode, ScrubModeScrub, StringComparison.CurrentCultureIgnoreCase))
                {
                    scrub = true;
                    outputSuffix = "-Scrub";
                }
                else if (string.Equals(ScrubMode, ScrubModeTrimmedScrub, StringComparison.CurrentCultureIgnoreCase))
                {
                    scrub = true;
                    trimmedScrub = true;
                    outputSuffix = "-TrimmedScrub";
                }
                else if (!string.Equals(ScrubMode, ScrubModeNone, StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new OptionException("Scrub mode is not valid.", "scrub");
                }

                var compressValue = CompressOptionType.None;
                if (string.Equals(CompressType, "NONE", StringComparison.CurrentCultureIgnoreCase))
                {
                    compressValue = CompressOptionType.None;
                }
                else if (string.Equals(CompressType, "CCI", StringComparison.CurrentCultureIgnoreCase))
                {
                    compressValue = CompressOptionType.CCI;
                }
                else if (string.Equals(CompressType, "CSO", StringComparison.CurrentCultureIgnoreCase))
                {
                    compressValue = CompressOptionType.CSO;
                }
                else
                {
                    throw new OptionException("compress is not valid.", "compress");
                }

                System.Console.WriteLine("Converting:");
                var imageInput = ImageImputHelper.GetImageInput(input);
                foreach (var inputPart in imageInput.Parts)
                {
                    System.Console.WriteLine(Path.GetFileName(inputPart));
                }

                outputPath = Path.Combine(outputPath, $"Converted{outputSuffix}");
                Directory.CreateDirectory(outputPath);

                var previousProgress = -1.0f;

                if (outputPath != null)
                {
                    if (compressValue == CompressOptionType.CCI)
                    {
                        XisoUtility.CreateCCI(imageInput, outputPath, outputNameWithoutExtension, ".cci", scrub, trimmedScrub, (s, p) =>
                        {
                            var amount = (float)Math.Round(p * 100);
                            if (!Quiet && amount != previousProgress)
                            {
                                System.Console.Write($"Stage {s + 1} of 3, Progress {amount}%");
                                System.Console.CursorLeft = 0;
                                previousProgress = amount;
                            }
                        }, default);
                    }
                    if (compressValue == CompressOptionType.CSO)
                    {
                        XisoUtility.CreateCSO(imageInput, outputPath, outputNameWithoutExtension, ".cso", scrub, trimmedScrub, (s, p) =>
                        {
                            var amount = (float)Math.Round(p * 100);
                            if (!Quiet && amount != previousProgress)
                            {
                                System.Console.Write($"Stage {s + 1} of 3, Progress {amount}%");
                                System.Console.CursorLeft = 0;
                                previousProgress = amount;
                            }
                        }, default);
                    }
                    else
                    {
                        XisoUtility.Split(imageInput, outputPath, outputNameWithoutExtension, ".iso", scrub, trimmedScrub, NoSplit, (s, p) =>
                        {
                            var amount = (float)Math.Round(p * 100);
                            if (!Quiet && amount != previousProgress)
                            {
                                System.Console.Write($"Stage {s + 1} of 3, Progress {amount}%");
                                System.Console.CursorLeft = 0;
                                previousProgress = amount;
                            }
                        }, default);
                    }
                }

                if (!Quiet)
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine("Convert completed.");
                }
            }
            catch (OptionException e)
            {
                ConsoleUtil.ShowOptionException(e);
            }
        }

    }
}

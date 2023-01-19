using Mono.Options;
using Repackinator.Helpers;
using Resurgent.UtilityBelt.Library.Utilities.ImageInput;
using Resurgent.UtilityBelt.Library.Utilities;

namespace Repackinator.Console
{
    public static class ConsoleConvert 
    {
        public const string Action = "Convert";
        public static string Input { get; set; } = string.Empty;
        public static string ScrubMode { get; set; } = "NONE";
        public static bool Compress { get; set; } = false;
        public static bool ShowHelp { get; set; } = false;
        public static bool Wait { get; set; } = false;

        private static string ScrubModeNone = "None";
        private static string ScrubModeScrub = "Scrub";
        private static string ScrubModeTrimmedScrub = "TrimmedScrub";

        public static OptionSet GetOptions()
        {
            return new OptionSet {
                { "i|input=", "Input file", i => Input = i },
                { "s|scrub=", "Scrub mode (None *default*, Scrub, TrimmedScrub)", s => ScrubMode = s },
                { "c|compress", "Compress", c => Compress = c != null },
                { "h|help", "show help", h => ShowHelp = h != null },
                { "w|wait", "Wait on exit", w => Wait = w != null }
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

                System.Console.WriteLine("Converting:");
                var inputSlices = Utility.GetSlicesFromFile(input);
                foreach (var inputSlice in inputSlices)
                {
                    System.Console.WriteLine(Path.GetFileName(inputSlice));
                }

                outputPath = Path.Combine(outputPath, $"Converted{outputSuffix}");
                Directory.CreateDirectory(outputPath);

                var previousProgress = -1.0f;

                if (outputPath != null)
                {
                    if (Compress)
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
                ConsoleUtil.ShowOptionException(e);
            }
        }

    }
}

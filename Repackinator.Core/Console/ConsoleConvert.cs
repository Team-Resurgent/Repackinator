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
        public static bool Scrub { get; set; } = false;
        public static bool TrimScrub { get; set; } = false;
        public static bool Compress { get; set; } = false;
        public static bool NoSplit { get; set; } = false;
        public static bool ShowHelp { get; set; } = false;
        public static bool Wait { get; set; } = false;
        public static bool Quiet { get; set; } = false;

        public static OptionSet GetOptions()
        {
            return new OptionSet {
                { "i|input=", "Input file", i => Input = i },
                { "c|compress", "Compress (CCI)", c => Compress = c != null },
                { "s|scrub", "Scrub", s => Scrub = s != null },
                { "t|trim", "TrimScrub", t => TrimScrub = t != null },
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

                System.Console.WriteLine("Converting:");
                var imageInput = ImageImputHelper.GetImageInput(input);
                foreach (var inputPart in imageInput.Parts)
                {
                    System.Console.WriteLine(Path.GetFileName(inputPart));
                }

                outputPath = Path.Combine(outputPath, $"Converted");
                Directory.CreateDirectory(outputPath);

                var previousProgress = -1.0f;

                if (outputPath != null)
                {
                    if (Compress)
                    {
                        XisoUtility.CreateCCI(imageInput, outputPath, outputNameWithoutExtension, ".cci", (Scrub || TrimScrub), TrimScrub, (s, p) =>
                        {
                            var amount = (float)Math.Round(p * 100);
                            if (!Quiet && amount != previousProgress)
                            {
                                if (amount < 10)
                                {
                                    System.Console.Write($"Stage {s + 1} of 3, Progress   {amount}%");
                                }
                                else if (amount < 100)
                                {
                                    System.Console.Write($"Stage {s + 1} of 3, Progress  {amount}%");
                                }
                                else
                                {
                                    System.Console.Write($"Stage {s + 1} of 3, Progress {amount}%");
                                }
                                System.Console.CursorLeft = 0;
                                previousProgress = amount;
                            }
                        }, default);
                    } 
                    else
                    {
                        XisoUtility.Split(imageInput, outputPath, outputNameWithoutExtension, ".iso", (Scrub || TrimScrub), TrimScrub, NoSplit, (s, p) =>
                        {
                            var amount = (float)Math.Round(p * 100);
                            if (!Quiet && amount != previousProgress)
                            {
                                if (amount < 10)
                                {
                                    System.Console.Write($"Stage {s + 1} of 3, Progress   {amount}%");
                                }
                                else if (amount < 100)
                                {
                                    System.Console.Write($"Stage {s + 1} of 3, Progress  {amount}%");
                                }
                                else
                                {
                                    System.Console.Write($"Stage {s + 1} of 3, Progress {amount}%");
                                }
                                System.Console.CursorLeft = 0;
                                previousProgress = amount;
                            }
                        }, default);
                    }
                }

                if (!Quiet)
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine("Conversion completed.");
                }
            }
            catch (OptionException e)
            {
                ConsoleUtil.ShowOptionException(e);
            }
        }

    }
}

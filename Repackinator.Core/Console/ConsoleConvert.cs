using Mono.Options;
using Repackinator.Core.Helpers;
using XboxToolkit;
using XboxToolkit.Interface;
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
                var slices = ContainerUtility.GetSlicesFromFile(input);
                foreach (var slice in slices)
                {
                    System.Console.WriteLine(Path.GetFileName(slice));
                }

                outputPath = Path.Combine(outputPath, $"Converted");
                Directory.CreateDirectory(outputPath);

                var previousProgress = -1.0f;

                if (outputPath != null)
                {
                    if (!ContainerUtility.TryAutoDetectContainerType(input, out var containerReader) || containerReader == null)
                    {
                        throw new Exception("Unable to detect container type.");
                    }
                    using (containerReader)
                    {
                        if (!containerReader.TryMount())
                        {
                            throw new Exception("Unable to mount container.");
                        }
                        try
                        {
                            var processingOptions = ProcessingOptions.OneToOneCopy;
                            if (Scrub || TrimScrub) processingOptions |= ProcessingOptions.ScrubSectors;
                            if (TrimScrub) processingOptions |= ProcessingOptions.TrimSectors;
                            
                            if (Compress)
                            {
                                var outputFile = Path.Combine(outputPath, $"{outputNameWithoutExtension}.cci");
                                var progress = new Action<float>((p) =>
                                {
                                    var amount = (float)Math.Round(p * 100);
                                    if (!Quiet && amount != previousProgress)
                                    {
                                        if (amount < 10)
                                        {
                                            System.Console.Write($"Progress   {amount}%");
                                        }
                                        else if (amount < 100)
                                        {
                                            System.Console.Write($"Progress  {amount}%");
                                        }
                                        else
                                        {
                                            System.Console.Write($"Progress {amount}%");
                                        }
                                        System.Console.CursorLeft = 0;
                                        previousProgress = amount;
                                    }
                                });
                                
                                if (!ContainerUtility.ConvertContainerToCCI(containerReader, processingOptions, outputFile, 0, progress))
                                {
                                    throw new Exception("Unable to convert to CCI.");
                                }
                            } 
                            else
                            {
                                var outputFile = Path.Combine(outputPath, $"{outputNameWithoutExtension}.iso");
                                var splitPoint = NoSplit ? 0L : 4L * 1024 * 1024 * 1024; // 4GB
                                var progress = new Action<float>((p) =>
                                {
                                    var amount = (float)Math.Round(p * 100);
                                    if (!Quiet && amount != previousProgress)
                                    {
                                        if (amount < 10)
                                        {
                                            System.Console.Write($"Progress   {amount}%");
                                        }
                                        else if (amount < 100)
                                        {
                                            System.Console.Write($"Progress  {amount}%");
                                        }
                                        else
                                        {
                                            System.Console.Write($"Progress {amount}%");
                                        }
                                        System.Console.CursorLeft = 0;
                                        previousProgress = amount;
                                    }
                                });
                                
                                if (!ContainerUtility.ConvertContainerToISO(containerReader, processingOptions, outputFile, splitPoint, progress))
                                {
                                    throw new Exception("Unable to convert to ISO.");
                                }
                            }
                        }
                        finally
                        {
                            containerReader.Dismount();
                        }
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

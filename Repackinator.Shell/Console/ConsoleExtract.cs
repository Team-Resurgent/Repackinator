using Mono.Options;
using XboxToolkit;
using XboxToolkit.Interface;
using Repackinator.Core.Models;
using Repackinator.Core.Helpers;

namespace Repackinator.Shell.Console
{
    public static class ConsoleExtract
    {
        public const string Action = "Extract";
        public static string Input { get; set; } = string.Empty;
        public static string Output { get; set; } = string.Empty;
        public static bool ShowHelp { get; set; } = false;
        public static bool Wait { get; set; } = false;
        public static bool Quiet { get; set; } = false;

        public static OptionSet GetOptions()
        {
            return new OptionSet {
                { "i|input=", "Input file", i => Input = i },
                { "o|output=", "Output directory (optional, defaults to folder with input name)", o => Output = o },
                { "h|help", "show help", h => ShowHelp = h != null },
                { "w|wait", "Wait on exit", w => Wait = w != null },
                { "q|quiet", "Suppress status output", q => Quiet = q != null }
            };
        }

        public static void ShowOptionDescription()
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Extract Action...");
            System.Console.WriteLine();
            System.Console.WriteLine("This action is used to extract files from xbox disk image.");
            System.Console.WriteLine();
            GetOptions().WriteOptionDescriptions(System.Console.Out);
        }

        public static void Process(string version, string[] args)
        {
            var config = Config.LoadConfig();

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
                    throw new OptionException("Input file not specified.", "input");
                }

                if (!File.Exists(Input))
                {
                    throw new OptionException("Input file does not exist.", "input");
                }

                string outputPath;
                if (!string.IsNullOrEmpty(Output))
                {
                    outputPath = Path.GetFullPath(Output);
                    Directory.CreateDirectory(outputPath);
                }
                else
                {
                    // Default behavior: use folder with input name
                    var inputDir = Path.GetDirectoryName(Input);
                    if (inputDir == null)
                    {
                        throw new IOException("Unable to get directory name from input.");
                    }
                    var baseName = Path.GetFileNameWithoutExtension(Input);
                    var subExtension = Path.GetExtension(baseName);
                    if (subExtension.Equals(".1") || subExtension.Equals(".2"))
                    {
                        baseName = Path.GetFileNameWithoutExtension(baseName);
                    }
                    outputPath = Path.Combine(inputDir, baseName);
                    Directory.CreateDirectory(outputPath);
                }

                if (!Quiet)
                {
                    System.Console.WriteLine("Extracting From:");
                    var slices = ContainerUtility.GetSlicesFromFile(Input);
                    foreach (var slice in slices)
                    {
                        System.Console.WriteLine(Path.GetFileName(slice));
                    }
                    System.Console.WriteLine($"To: {outputPath}");
                    System.Console.WriteLine("Extracting...");
                }

                if (!ContainerUtility.TryAutoDetectContainerType(Input, out var containerReader) || containerReader == null)
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
                        var previousProgress = -1.0f;
                        var progress = new Action<float>((p) =>
                        {
                            var amount = (float)Math.Round(p * 100);
                            if (!Quiet && amount != previousProgress)
                            {
                                System.Console.Write($"Progress {amount}%");
                                System.Console.CursorLeft = 0;
                                previousProgress = amount;
                            }
                        });
                        
                        if (!ContainerUtility.ExtractFilesFromContainer(containerReader, outputPath))
                        {
                            throw new Exception("Unable to extract files.");
                        }
                    }
                    finally
                    {
                        containerReader.Dismount();
                    }
                }

                if (!Quiet)
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine("Extract completed.");
                }
            }
            catch (OptionException e)
            {
                ConsoleUtil.ShowOptionException(e, Action, version);
            }
        }
    }
}


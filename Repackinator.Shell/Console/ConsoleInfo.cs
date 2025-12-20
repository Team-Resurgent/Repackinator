using Mono.Options;
using Repackinator.Core.Helpers;
using XboxToolkit;
using XboxToolkit.Interface;
using Repackinator.Core.Models;

namespace Repackinator.Shell.Console
{
    public static class ConsoleInfo
    {
        public const string Action = "Info";
        public static string Input { get; set; } = string.Empty;
        public static string Output { get; set; } = string.Empty;
        public static bool ShowHelp { get; set; } = false;
        public static bool Wait { get; set; } = false;
        public static bool Quiet { get; set; } = false;

        public static OptionSet GetOptions()
        {
            return new OptionSet {
                { "i|input=", "Input file", i => Input = i },
                { "o|output=", "Output file (optional, defaults to console)", o => Output = o },
                { "h|help", "show help", h => ShowHelp = h != null },
                { "w|wait", "Wait on exit", w => Wait = w != null },
                { "q|quiet", "Suppress status output", q => Quiet = q != null }
            };
        }

        public static void ShowOptionDescription()
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Info Action...");
            System.Console.WriteLine();
            System.Console.WriteLine("This action is used to show xbox disk data sector information.");
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

                StreamWriter? outputWriter = null;
                if (!string.IsNullOrEmpty(Output))
                {
                    var outputPath = Path.GetFullPath(Output);
                    var outputDir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
                    outputWriter = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
                }

                try
                {
                    if (!Quiet)
                    {
                        System.Console.WriteLine("Getting Info From:");
                        var slices = ContainerUtility.GetSlicesFromFile(Input);
                        foreach (var slice in slices)
                        {
                            System.Console.WriteLine(Path.GetFileName(slice));
                        }
                        System.Console.WriteLine();
                    }

                    var headerLine = $"Type,Filename,Size,StartSector,EndSector,InSlices";
                    if (outputWriter != null)
                    {
                        outputWriter.WriteLine(headerLine);
                    }
                    else
                    {
                        System.Console.WriteLine(headerLine);
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

                            ContainerUtility.GetFileInfoFromContainer(containerReader, f =>
                            {
                                var type = f.IsFile ? "F" : "D";
                                var startSector = f.StartSector > 0 ? f.StartSector.ToString() : "N/A";
                                var endSector = f.EndSector > 0 ? f.EndSector.ToString() : "N/A";
                                var line = $"{type},{f.Filename},{f.Size},{startSector},{endSector},{f.InSlices}";
                                if (outputWriter != null)
                                {
                                    outputWriter.WriteLine(line);
                                }
                                else
                                {
                                    System.Console.WriteLine(line);
                                }
                            }, progress, default);
                        }
                        finally
                        {
                            containerReader.Dismount();
                        }
                    }

                    if (!Quiet)
                    {
                        System.Console.WriteLine();
                        if (outputWriter != null)
                        {
                            System.Console.WriteLine($"Info saved to: {Output}");
                        }
                        System.Console.WriteLine("Info completed.");
                    }
                }
                finally
                {
                    outputWriter?.Dispose();
                }
            }
            catch (OptionException e)
            {
                ConsoleUtil.ShowOptionException(e, Action, version);
            }

            ConsoleUtil.ProcessWait(Wait);
        }
    }
}


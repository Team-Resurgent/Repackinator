using Mono.Options;
using Repackinator.Core.Helpers;
using XboxToolkit;
using XboxToolkit.Interface;
using Repackinator.Core.Models;

namespace Repackinator.Shell.Console
{
    public static class ConsoleChecksum
    {
        public const string Action = "Checksum";
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
            System.Console.WriteLine("Checksum Action...");
            System.Console.WriteLine();
            System.Console.WriteLine("This action is used to checksum xbox disk image sectors after any decompression if applicable.");
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

                if (!File.Exists(Input))
                {
                    throw new OptionException("Input is not a valid file.", "input");
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
                        System.Console.WriteLine("Calculating Checksum From:");
                        var slices = ContainerUtility.GetSlicesFromFile(Input);
                        foreach (var slice in slices)
                        {
                            System.Console.WriteLine(Path.GetFileName(slice));
                        }
                        System.Console.WriteLine("Calculating Checksums...");
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
                            var result = ContainerUtility.GetChecksumFromContainer(containerReader, p =>
                            {
                                var amount = (float)Math.Round(p * 100);
                                if (!Quiet && amount != previousProgress)
                                {
                                    System.Console.Write($"Progress {amount}%");
                                    System.Console.CursorLeft = 0;
                                    previousProgress = amount;
                                }
                            }, default);
                            
                            var checksumLine = $"SHA256 = {result}";
                            if (outputWriter != null)
                            {
                                outputWriter.WriteLine(result);
                            }
                            else
                            {
                                System.Console.WriteLine(checksumLine);
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
                        if (outputWriter != null)
                        {
                            System.Console.WriteLine($"Checksum saved to: {Output}");
                        }
                        System.Console.WriteLine("Checksum completed.");
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
        }
    }
}


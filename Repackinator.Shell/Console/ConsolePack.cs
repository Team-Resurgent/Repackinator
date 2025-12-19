using Mono.Options;
using XboxToolkit;
using System.Text;

namespace Repackinator.Shell.Console
{
    public static class ConsolePack
    {
        public const string Action = "Pack";
        public static string Input { get; set; } = string.Empty;
        public static string Output { get; set; } = string.Empty;
        public static bool Compress { get; set; } = false;
        public static bool NoSplit { get; set; } = false;
        public static string Log { get; set; } = string.Empty;
        public static bool ShowHelp { get; set; } = false;
        public static bool Wait { get; set; } = false;
        public static bool Quiet { get; set; } = false;

        public static OptionSet GetOptions()
        {
            return new OptionSet {
                { "i|input=", "Input folder", i => Input = i },
                { "o|output=", "Output file", o => Output = o },
                { "c|compress", "Compress (CCI)", c => Compress = c != null },
                { "n|nosplit", "No Split of output file", n => NoSplit = n != null },
                { "l|log=", "log file", l => Log = l },
                { "h|help", "show help", h => ShowHelp = h != null },
                { "w|wait", "Wait on exit", w => Wait = w != null },
                { "q|quiet", "Suppress status output", q => Quiet = q != null }
            };
        }

        public static void ShowOptionDescription()
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Pack Action...");
            System.Console.WriteLine();
            System.Console.WriteLine("This action is used to pack a folder into an Xbox disk image (ISO or CCI).");
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
                    throw new OptionException("output is not a valid filepath.", "output");
                }

                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(output);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Repackinator is Xbox Original only
                var formatValue = ISOFormat.XboxOriginal;

                // Determine output extension based on compress option
                if (Compress)
                {
                    if (!output.EndsWith(".cci", StringComparison.OrdinalIgnoreCase))
                    {
                        output = Path.ChangeExtension(output, ".cci");
                    }
                }
                else
                {
                    if (!output.EndsWith(".iso", StringComparison.OrdinalIgnoreCase))
                    {
                        output = Path.ChangeExtension(output, ".iso");
                    }
                }

                // Split point is always 4GB unless NoSplit is specified
                var splitPoint = NoSplit ? 0L : 4L * 1024 * 1024 * 1024; // 4GB

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

                var logger = new Action<string>((message) =>
                {
                    if (!Quiet)
                    {
                        System.Console.WriteLine(message);
                    }
                    if (logStream != null)
                    {
                        var logMessage = $"{DateTime.Now:HH:mm:ss} - {message}\r\n";
                        logStream.Write(Encoding.UTF8.GetBytes(logMessage));
                    }
                });

                if (!Quiet)
                {
                    System.Console.WriteLine("Packing folder:");
                    System.Console.WriteLine(input);
                    System.Console.WriteLine("To:");
                    System.Console.WriteLine(output);
                    System.Console.WriteLine($"Type: {(Compress ? "CCI" : "ISO")}");
                    System.Console.WriteLine();
                }

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

                bool success;
                if (Compress)
                {
                    success = ContainerUtility.ConvertFolderToCCI(input, formatValue, output, splitPoint, progress);
                }
                else
                {
                    success = ContainerUtility.ConvertFolderToISO(input, formatValue, output, splitPoint, progress);
                }

                logStream?.Dispose();

                if (!Quiet)
                {
                    System.Console.WriteLine();
                    if (success)
                    {
                        System.Console.WriteLine("Pack completed successfully.");
                    }
                    else
                    {
                        System.Console.WriteLine("Pack failed.");
                    }
                }

                if (!success)
                {
                    Environment.ExitCode = 1;
                }
            }
            catch (OptionException e)
            {
                ConsoleUtil.ShowOptionException(e);
                Environment.ExitCode = 1;
            }
            catch (Exception e)
            {
                if (!Quiet)
                {
                    System.Console.WriteLine($"Error: {e.Message}");
                }
                Environment.ExitCode = 1;
            }

            ConsoleUtil.ProcessWait(Wait);
        }
    }
}


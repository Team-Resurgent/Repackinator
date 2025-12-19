using Mono.Options;
using XboxToolkit;
using XboxToolkit.Interface;
using XboxToolkit.Models.Xbe;
using XboxToolkit.Internal;

namespace Repackinator.Shell.Console
{
    public static class ConsoleXbeInfo
    {
        public const string Action = "XbeInfo";
        public static string Input { get; set; } = string.Empty;
        public static string Output { get; set; } = string.Empty;
        public static bool ShowHelp { get; set; } = false;
        public static bool Wait { get; set; } = false;
        public static bool Quiet { get; set; } = false;

        public static OptionSet GetOptions()
        {
            return new OptionSet {
                { "i|input=", "Input file (ISO or CCI)", i => Input = i },
                { "o|output=", "Output file (optional, defaults to console)", o => Output = o },
                { "h|help", "show help", h => ShowHelp = h != null },
                { "w|wait", "Wait on exit", w => Wait = w != null },
                { "q|quiet", "Suppress status output", q => Quiet = q != null }
            };
        }

        public static void ShowOptionDescription()
        {
            System.Console.WriteLine();
            System.Console.WriteLine("XbeInfo Action...");
            System.Console.WriteLine();
            System.Console.WriteLine("This action extracts XBE certificate information from Xbox disk images.");
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
                        System.Console.WriteLine("Extracting XBE Info From:");
                        var slices = ContainerUtility.GetSlicesFromFile(Input);
                        foreach (var slice in slices)
                        {
                            System.Console.WriteLine(Path.GetFileName(slice));
                        }
                        System.Console.WriteLine();
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
                            if (!containerReader.TryGetDefault(out var xbeData, out var containerType))
                            {
                                throw new Exception("Unable to extract XBE from container.");
                            }

                            if (containerType != ContainerType.XboxOriginal)
                            {
                                throw new Exception("Input is not an Xbox Original disk image.");
                            }

                            if (!XbeUtility.TryGetXbeCert(xbeData, out var cert) || cert == null)
                            {
                                throw new Exception("Unable to extract XBE certificate.");
                            }

                            Action<string> writeLine = (line) =>
                            {
                                if (outputWriter != null)
                                {
                                    outputWriter.WriteLine(line);
                                }
                                else
                                {
                                    System.Console.WriteLine(line);
                                }
                            };

                            writeLine("XBE Certificate Information:");
                            writeLine("============================");
                            var certInstance = new XbeCertificate();
                            writeLine($"Title ID: {cert.Title_Id:X08}");
                            writeLine($"Title Name: {UnicodeHelper.GetUnicodeString(cert.Title_Name)}");
                            writeLine($"Game Region: {XbeCertificate.GameRegionToString(cert.Game_Region)}");
                            writeLine($"Version: {cert.Version:X08}");
                            writeLine($"Allowed Media: {certInstance.AllowedMediaToString(cert.Allowed_Media)}");
                            writeLine($"Game Ratings: {cert.Game_Ratings:X08}");
                            writeLine($"Disk Number: {cert.Disk_Number}");
                            writeLine($"Time Date: {cert.Time_Date:X08}");
                            
                            if (!Quiet && outputWriter == null)
                            {
                                System.Console.WriteLine();
                                System.Console.WriteLine("XbeInfo completed.");
                            }
                            else if (outputWriter != null && !Quiet)
                            {
                                System.Console.WriteLine($"XBE info saved to: {Output}");
                            }
                        }
                        finally
                        {
                            containerReader.Dismount();
                        }
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


using Mono.Options;
using Repackinator.Core.Helpers;
using XboxToolkit;
using XboxToolkit.Interface;
using Repackinator.Core.Models;

namespace Repackinator.Shell.Console
{
    public static class ConsoleCompare
    {
        public const string Action = "Compare";
        public static string First { get; set; } = string.Empty;
        public static string Second { get; set; } = string.Empty;
        public static bool Compare { get; set; } = false;
        public static bool ShowHelp { get; set; } = false;
        public static bool Wait { get; set; } = false;

        public static OptionSet GetOptions()
        {
            return new OptionSet {
                { "f|first=", "First file to compare", f => First = f },
                { "s|second=", "Second file to compare", s => Second = s },
                { "c|compare", "Perform comparison (requires both -f and -s)", c => Compare = c != null },
                { "h|help", "show help", h => ShowHelp = h != null },
                { "w|wait", "Wait on exit", w => Wait = w != null }
            };
        }

        public static void ShowOptionDescription()
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Compare Action...");
            System.Console.WriteLine();
            System.Console.WriteLine("This action is used to compare two Xbox disk images sector by sector.");
            System.Console.WriteLine("You can specify both files with -f and -s, or use -c to compare files previously set.");
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

                // If no parameters provided, show help
                if (string.IsNullOrEmpty(First) && string.IsNullOrEmpty(Second) && !Compare)
                {
                    throw new OptionException("No parameters specified. Use -f to set first file, -s to set second file, or -c to compare.", "compare");
                }

                string? firstFile = null;
                string? secondFile = null;

                // Load config to get/set comparison files
                var config = Config.LoadConfig();

                // Process first file: save to config if provided, or load from config if not provided
                if (!string.IsNullOrEmpty(First))
                {
                    if (!File.Exists(First))
                    {
                        throw new OptionException("First is not a valid file.", "first");
                    }
                    firstFile = Path.GetFullPath(First);
                    // Save to config for future use
                    if (config != null)
                    {
                        config.CompareFirst = firstFile;
                        Config.SaveConfig(config);
                    }
                }
                else if (config != null && !string.IsNullOrEmpty(config.CompareFirst))
                {
                    // Load from config if not provided via command line
                    firstFile = config.CompareFirst;
                }

                // Process second file: save to config if provided, or load from config if not provided
                if (!string.IsNullOrEmpty(Second))
                {
                    if (!File.Exists(Second))
                    {
                        throw new OptionException("Second is not a valid file.", "second");
                    }
                    secondFile = Path.GetFullPath(Second);
                    // Save to config for future use
                    if (config != null)
                    {
                        config.CompareSecond = secondFile;
                        Config.SaveConfig(config);
                    }
                }
                else if (config != null && !string.IsNullOrEmpty(config.CompareSecond))
                {
                    // Load from config if not provided via command line
                    secondFile = config.CompareSecond;
                }

                // If compare flag is set, ensure we have both files (from command line or config)
                if (Compare)
                {
                    if (string.IsNullOrEmpty(firstFile) || string.IsNullOrEmpty(secondFile))
                    {
                        throw new OptionException("Both first and second files must be specified (use -f to set first, or it will be loaded from config).", "compare");
                    }
                }

                if (!string.IsNullOrEmpty(firstFile) && !string.IsNullOrEmpty(secondFile))
                {
                    if (!File.Exists(firstFile))
                    {
                        throw new OptionException("First file does not exist.", "first");
                    }

                    if (!File.Exists(secondFile))
                    {
                        throw new OptionException("Second file does not exist.", "second");
                    }

                    System.Console.WriteLine("Comparing:");
                    var firstSlices = ContainerUtility.GetSlicesFromFile(firstFile);
                    foreach (var slice in firstSlices)
                    {
                        System.Console.WriteLine(Path.GetFileName(slice));
                    }

                    System.Console.WriteLine("Against:");
                    var secondSlices = ContainerUtility.GetSlicesFromFile(secondFile);
                    foreach (var slice in secondSlices)
                    {
                        System.Console.WriteLine(Path.GetFileName(slice));
                    }

                    System.Console.WriteLine();

                    if (!ContainerUtility.TryAutoDetectContainerType(firstFile, out var containerReader1) || containerReader1 == null)
                    {
                        throw new Exception("Unable to detect container type for first file.");
                    }
                    if (!ContainerUtility.TryAutoDetectContainerType(secondFile, out var containerReader2) || containerReader2 == null)
                    {
                        containerReader1.Dispose();
                        throw new Exception("Unable to detect container type for second file.");
                    }
                    
                    using (containerReader1)
                    using (containerReader2)
                    {
                        if (!containerReader1.TryMount())
                        {
                            throw new Exception("Unable to mount first container.");
                        }
                        if (!containerReader2.TryMount())
                        {
                            containerReader1.Dismount();
                            throw new Exception("Unable to mount second container.");
                        }
                        try
                        {
                            var previousProgress = -1.0f;
                            ContainerUtility.CompareContainers(containerReader1, containerReader2, s =>
                            {
                                System.Console.WriteLine(s);
                            }, p =>
                            {
                                var amount = (float)Math.Round(p * 100);
                                if (amount != previousProgress)
                                {
                                    System.Console.Write($"Progress {amount}%");
                                    System.Console.CursorLeft = 0;
                                    previousProgress = amount;
                                }
                            });
                        }
                        finally
                        {
                            containerReader1.Dismount();
                            containerReader2.Dismount();
                        }
                    }

                    System.Console.WriteLine();
                    System.Console.WriteLine("Compare completed.");
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


using Mono.Options;
using Repackinator.Core.Helpers;
using XboxToolkit;
using XboxToolkit.Interface;
using Repackinator.Core.Models;

namespace Repackinator.Core.Console
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
                { "f|first=", "Set first file to compare", f => First = f },
                { "s|second=", "Set second file to compare", s => Second = s },
                { "c|compare", "Compare", c => Compare = c != null },
                { "h|help", "show help", h => ShowHelp = h != null },
                { "w|wait", "Wait on exit", w => Wait = w != null }
            };
        }

        public static void ShowOptionDescription()
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Compare Action...");
            System.Console.WriteLine();
            System.Console.WriteLine("This action is used to compare one xbox disk image with another.");
            System.Console.WriteLine();
            GetOptions().WriteOptionDescriptions(System.Console.Out);
        }

        public static void Process(string version, string[] args)
        {
            var config = Config.LoadConfig();
            if (config == null)
            {
                throw new Exception("Config error.");
            }

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

                if (!string.IsNullOrEmpty(First))
                {
                    if (!File.Exists(First))
                    {
                        throw new OptionException("First is not a valid file.", "first");
                    }
                    config.CompareFirst = Path.GetFullPath(First);
                    Config.SaveConfig(config);
                }

                if (!string.IsNullOrEmpty(Second))
                {
                    if (!File.Exists(Second))
                    {
                        throw new OptionException("Second is not a valid file.", "second");
                    }
                    config.CompareSecond = Path.GetFullPath(Second);
                    Config.SaveConfig(config);
                }

                if (Compare || !string.IsNullOrEmpty(First) && !string.IsNullOrEmpty(Second))
                {
                    if (!File.Exists(config.CompareFirst))
                    {
                        throw new OptionException("First is not a valid file.", "input");
                    }

                    if (!File.Exists(config.CompareSecond))
                    {
                        throw new OptionException("Second is not a valid file.", "input");
                    }

                    System.Console.WriteLine("Comparing:");
                    var firstSlices = ContainerUtility.GetSlicesFromFile(config.CompareFirst);
                    foreach (var slice in firstSlices)
                    {
                        System.Console.WriteLine(Path.GetFileName(slice));
                    }

                    System.Console.WriteLine("Against:");
                    var secondSlices = ContainerUtility.GetSlicesFromFile(config.CompareSecond);
                    foreach (var slice in secondSlices)
                    {
                        System.Console.WriteLine(Path.GetFileName(slice));
                    }

                    System.Console.WriteLine();

                    if (!ContainerUtility.TryAutoDetectContainerType(config.CompareFirst, out var containerReader1) || containerReader1 == null)
                    {
                        throw new Exception("Unable to detect container type for first file.");
                    }
                    if (!ContainerUtility.TryAutoDetectContainerType(config.CompareSecond, out var containerReader2) || containerReader2 == null)
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
                ConsoleUtil.ShowOptionException(e);
            }
        }
    }
}

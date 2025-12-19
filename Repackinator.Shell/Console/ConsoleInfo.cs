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
        public static bool ShowHelp { get; set; } = false;
        public static bool Wait { get; set; } = false;

        public static OptionSet GetOptions()
        {
            return new OptionSet {
                { "i|input=", "Input file", i => Input = i },
                { "h|help", "show help", h => ShowHelp = h != null },
                { "w|wait", "Wait on exit", w => Wait = w != null }
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

                if (!File.Exists(Input))
                {
                    throw new OptionException("Input is not a valid file.", "input");
                }

                System.Console.WriteLine("Getting Info From:");
                var slices = ContainerUtility.GetSlicesFromFile(Input);
                foreach (var slice in slices)
                {
                    System.Console.WriteLine(Path.GetFileName(slice));
                }

                System.Console.WriteLine("Processing...");
                System.Console.WriteLine($"Type,Filename,Size,StartSector,EndSector,InSlices");
                
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
                        ContainerUtility.GetFileInfoFromContainer(containerReader, f =>
                        {
                            var type = f.IsFile ? "F" : "D";
                            var startSector = f.StartSector > 0 ? f.StartSector.ToString() : "N/A";
                            var endSector = f.EndSector > 0 ? f.EndSector.ToString() : "N/A";
                            System.Console.WriteLine($"{type},{f.Filename},{f.Size},{startSector},{endSector},{f.InSlices}");
                        }, null, default);
                    }
                    finally
                    {
                        containerReader.Dismount();
                    }
                }

                System.Console.WriteLine();
                System.Console.WriteLine("Info completed.");
            }
            catch (OptionException e)
            {
                ConsoleUtil.ShowOptionException(e, Action, version);
            }

            ConsoleUtil.ProcessWait(Wait);
        }
    }
}


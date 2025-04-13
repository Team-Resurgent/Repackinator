using Mono.Options;
using Repackinator.Core.Helpers;
using Resurgent.UtilityBelt.Library.Utilities.ImageInput;
using Resurgent.UtilityBelt.Library.Utilities;
using Repackinator.Core.Models;

namespace Repackinator.Core.Console
{
    public static class ConsoleChecksum
    {
        public const string Action = "Checksum";
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

                System.Console.WriteLine("Calculating Checksum From:");
                var imageInput = ImageImputHelper.GetImageInput(Input);
                foreach (var part in imageInput.Parts)
                {
                    System.Console.WriteLine(Path.GetFileName(part));
                }

                var previousProgress = -1.0f;

                System.Console.WriteLine("alculating Checksums...");
                var result = XisoUtility.GetChecksumFromXiso(imageInput, p =>
                {
                    var amount = (float)Math.Round(p * 100);
                    if (amount != previousProgress)
                    {
                        System.Console.Write($"Progress {amount}%");
                        System.Console.CursorLeft = 0;
                        previousProgress = amount;
                    }
                }, default);
                System.Console.WriteLine($"SHA256 = {result}");

                System.Console.WriteLine();
                System.Console.WriteLine("Checksum completed.");
            }
            catch (OptionException e)
            {
                ConsoleUtil.ShowOptionException(e);
            }
        }
    }
}

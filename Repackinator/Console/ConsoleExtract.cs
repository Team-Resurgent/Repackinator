using Mono.Options;
using Repackinator.Helpers;
using Resurgent.UtilityBelt.Library.Utilities.ImageInput;
using Resurgent.UtilityBelt.Library.Utilities;
using Repackinator.Models;

namespace Repackinator.Console
{
    public static class ConsoleExtract
    {
        public const string Action = "Extract";
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

                if (!File.Exists(Input))
                {
                    throw new OptionException("Input is not a valid file.", "input");
                }

                System.Console.WriteLine("Calculating Checksum From:");
                var imageInput = ImageImputHelper.GetImageInput(Input);
                foreach (var inputPart in imageInput.Parts)
                {
                    System.Console.WriteLine(Path.GetFileName(inputPart));
                }

                System.Console.WriteLine("Extracting...");

                var outputPath = Path.GetDirectoryName(Input);
                if (outputPath == null)
                {
                    throw new IOException("Unable to get directory name from input.");
                }
                outputPath = Path.Combine(outputPath, Utility.GetNameFromSlice(Input));
                Directory.CreateDirectory(outputPath);

                var previousProgress = -1.0f;
                XisoUtility.GetFileInfoFromXiso(imageInput, f => {

                    if (!f.IsFile)
                    {
                        return;
                    }

                    var sector = f.StartSector;
                    var size = f.Size;
                    var result = new byte[size];
                    var processed = 0U;
                    if (size > 0)
                    {
                        while (processed < size)
                        {
                            var buffer = imageInput.ReadSectors(sector, 1);
                            var bytesToCopy = (uint)Math.Min(size - processed, 2048);
                            Array.Copy(buffer, 0, result, processed, bytesToCopy);
                            sector++;
                            processed += bytesToCopy;
                        }
                    }
                    var destPath = Path.Combine(outputPath, f.Path);
                    Directory.CreateDirectory(destPath);
                    var fileName = Path.Combine(destPath, f.Filename);
                    File.WriteAllBytes(fileName, result);

                },
                p =>
                {
                    var amount = (float)Math.Round(p * 100);
                    if (amount != previousProgress)
                    {
                        System.Console.Write($"Progress {amount}%");
                        System.Console.CursorLeft = 0;
                        previousProgress = amount;
                    }
                }, default);

                System.Console.WriteLine();
                System.Console.WriteLine("Extract completed.");
            }
            catch (OptionException e)
            {
                ConsoleUtil.ShowOptionException(e);
            }
        }
    }
}

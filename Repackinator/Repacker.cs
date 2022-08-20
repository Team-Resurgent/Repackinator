using System.Diagnostics;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Repackinator;
using Resurgent.UtilityBelt.Library.Utilities;
using Resurgent.UtilityBelt.Library.Utilities.XbeModels;

namespace QuikIso
{
    public static class Repacker
    {
        private static FileStream? LogStream { get; set; }

        private static string? TempFolder { get; set; }

        private static string? SevenZipFile { get; set; }

        private static GameData[]? GameData { get; set; }

        private static void Log(string message)
        {
            Console.WriteLine(message);
            if (LogStream == null)
            {
                return;
            }
            var bytes = Encoding.UTF8.GetBytes(message);
            LogStream.Write(bytes);
        }

        private static void ProcessFile(string inputFile, string outputPath, string grouping)
        {
            if (TempFolder == null)
            {
                Log($"Error: TempFolder should not be null.");
                return;
            }

            if (SevenZipFile == null)
            {
                Log($"Error: SevenZipFile should not be null.");
                return;
            }

            if (GameData == null)
            {
                Log($"Error: GameData should not be null.");
                return;
            }

            var unpackPath = Path.Combine(TempFolder, "Unpack");

            try
            {
                if (!File.Exists(inputFile))
                {
                    Log($"Skipping '{Path.GetFileName(inputFile)}' as does not exist.");
                    return;
                }

                var extension = Path.GetExtension(inputFile).ToLower();
                if (!extension.Equals(".iso") && !extension.Equals(".zip") && !extension.Equals(".iso"))
                {
                    Log($"Skipping '{Path.GetFileName(inputFile)}' as unsupported extension.");
                    return;
                }

                Log($"Processing '{Path.GetFileName(inputFile)}'...");

                if (!Directory.Exists(unpackPath))
                {
                    Directory.CreateDirectory(unpackPath);
                }

                var unpacked = false;

                var input = inputFile;
                if (!extension.Equals(".iso"))
                {

                    Log("Extracting Archive...");
                    var processList = new Process
                    {
                        StartInfo = new ProcessStartInfo(SevenZipFile)
                        {
                            Arguments = $"-ba -slt l \"{inputFile}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true
                        },
                    };
                    processList.Start();
                    var outputList = processList.StandardOutput.ReadToEnd();
                    processList.WaitForExit();
                    if (processList.ExitCode != 0)
                    {
                        Log("Error: failed to get archive info.");
                        return;
                    }

                    input = $"{Path.GetFileNameWithoutExtension(inputFile)}.iso";
                    var outputLines = outputList.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in outputLines)
                    {
                        if (line.StartsWith("Path = "))
                        {
                            input = line.Substring(7);
                            break;
                        }
                    }
                    input = Path.Combine(unpackPath, input);

                    using var process = new Process
                    {
                        StartInfo = new ProcessStartInfo(SevenZipFile)
                        {
                            Arguments = $"x -y -o\"{unpackPath}\" \"{inputFile}\"", //input file is the zip
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        },
                    };
                    process.Start();
                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        Log("Error: failed to extract archive.");
                        return;
                    }

                    unpacked = true;
                }

                var xbeData = Array.Empty<byte>();
                using (var inputStream = new FileStream(input, FileMode.Open))
                using (var outputStream = new MemoryStream())
                {
                    var error = string.Empty;
                    if (XisoUtility.TryExtractDefaultFromXiso(inputStream, outputStream, ref error))
                    {
                        xbeData = outputStream.ToArray();
                    }
                    else
                    {
                        Log($"Error: Unable to extract default.xbe.");
                        if (unpacked)
                        {
                            File.Delete(input);
                        }
                        return;
                    }
                }

                if (!XbeUtility.TryGetXbeCert(xbeData, out var cert) || cert == null)
                {
                    Log($"Error: Unable to get data from default.xbe.");
                    if (unpacked)
                    {
                        File.Delete(input);
                    }
                    return;
                }

                
                var titleId = cert.Value.Title_Id.ToString("X2");
                var gameRegion = XbeCertificate.GameRegionToString(cert.Value.Game_Region);
                var version = cert.Value.Version.ToString("X2");

                bool found = false;

                GameData? gameData = null;
                foreach (var game in GameData)
                {
                    if (game.TitleID == titleId && game.Region == gameRegion && game.Version == version)
                    {
                        found = true;
                        if (game?.Process != null && game.Process.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
                        {
                            gameData = game;
                        }
                        break;
                    }
                } 

                if (gameData == null)
                {
                    if (found)
                    {
                        Log($"Skipping '{Path.GetFileName(inputFile)}' as requested to skip in dataset.");
                    }
                    else
                    {
                        Log($"Skipping '{Path.GetFileName(inputFile)}' as titleid, region and version not found in dataset.");
                    }
                    return;
                }

                if (gameData.Region == null)
                {
                    Log($"Error: region is null in dataset.");
                    return;
                }

                if (gameData.XBETitleAndFolderName == null)
                {
                    Log($"Error: XBE title & folder name is null in dataset.");
                    return;
                }

                if (gameData.ISOName == null)
                {
                    Log($"Error: ISO name is null in dataset.");
                    return;
                }

                if (gameData.Letter == null)
                {
                    Log($"Error: Letter is null in dataset.");
                    return;
                }

                if (string.Equals(grouping, "REGION"))
                {
                    outputPath = Path.Combine(outputPath, gameData.Region);
                }
                else if (string.Equals(grouping, "LETTER"))
                {
                    outputPath = Path.Combine(outputPath, gameData.XBETitleAndFolderName[0].ToString().ToUpper());
                }
                else if (string.Equals(grouping, "REGIONLETTER"))
                {
                    outputPath = Path.Combine(outputPath, gameData.Region, gameData.Letter);
                }
                else if (string.Equals(grouping, "LETTERREGION"))
                {
                    outputPath = Path.Combine(outputPath, gameData.Letter, gameData.Region);
                }

                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                Directory.CreateDirectory(Path.Combine(outputPath, gameData.XBETitleAndFolderName));

                var attach = ResourceLoader.GetEmbeddedResourceBytes("attach.xbe");
                if (XbeUtility.TryGetXbeImage(xbeData, XbeUtility.ImageType.TitleImage, out var xprImage))
                {
                    if (XprUtility.ConvertXprToPng(xprImage, out var pngImage))
                    {
                        if (!XbeUtility.TryReplaceXbeTitleImage(attach, pngImage))
                        {
                            Log($"Error: failed to replace image.");
                            if (unpacked)
                            {
                                File.Delete(input);
                            }
                            return;
                        }
                    }
                    else
                    {
                        Log($"Error: failed to create png.");
                        if (unpacked)
                        {
                            File.Delete(input);
                        }
                        return;
                    }
                }
                else
                {
                    Log($"Error: failed to extract xpr.");
                    if (unpacked)
                    {
                        File.Delete(input);
                    }
                    return;
                }
                                                
                if (XbeUtility.ReplaceCertInfo(attach, xbeData, gameData.XBETitleAndFolderName, out var patchedAttach) && patchedAttach != null)
                {
                    File.WriteAllBytes(Path.Combine(outputPath, gameData.XBETitleAndFolderName, $"default.xbe"), patchedAttach);
                }
                else
                {
                    Log($"Error: failed creating attach xbe.");
                    if (unpacked)
                    {
                        File.Delete(input);
                    }
                    return;
                }

                Log("Removing Video Partition & Splitting ISO...");
                XisoUtility.Split($"{input}", Path.Combine(outputPath, gameData.XBETitleAndFolderName), gameData.ISOName, true);

                if (unpacked)
                {
                    File.Delete(input);
                }
            }
            catch (Exception ex)
            {
                Log($"Error Processing '{inputFile}' with error '{ex}'.");
            }
        }

        public static void StartConversion(string input, string output, string grouping, string temp, string log)
        {
            FileStream? logStream = null;

            try
            {
                if (!string.IsNullOrEmpty(log))
                {
                    LogStream = File.OpenWrite(log);
                }

                var exePath = AppDomain.CurrentDomain.BaseDirectory;
                if (exePath == null)
                {
                    Log("Error: Unable to get path of executable.");
                    return;
                }

                var repackPath = Path.GetDirectoryName(exePath);
                if (repackPath == null)
                {
                    Log("Error: Unable to get path from executable parg.");
                    return;
                }

                var repackList = Path.Combine(repackPath, "RepackList.json");
                if (!File.Exists(repackList))
                {
                    Log("Error: RepackList.json not found.");
                    return;
                }

                var gameDataJson = File.ReadAllText(repackList);
                GameData = JsonConvert.DeserializeObject<GameData[]>(gameDataJson);

                TempFolder = temp;

                var sevenZipBytes = ResourceLoader.GetEmbeddedResourceBytes("7za.exe");
                var sevenZipFile = Path.Combine(temp, "7za.exe");
                try
                {
                    File.WriteAllBytes(sevenZipFile, sevenZipBytes);
                }
                catch
                {
                    // do nothing
                }
                SevenZipFile = sevenZipFile;

                var files = Directory.GetFiles(input);                
                foreach (var file in files)
                {
                    ProcessFile(file, output, grouping);                    
                }
            } 
            finally
            {
                if (logStream != null)
                {
                    logStream.Dispose();
                }
            }

        }
    }
}

﻿using System.Diagnostics;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Repackinator;
using Resurgent.UtilityBelt.Library.Utilities;

namespace QuikIso
{
    public static class Repacker
    {
        private static FileStream? LogStream { get; set; }

        private static string TempFolder { get; set; }

        private static string SevenZipFile { get; set; }

        private static GameData[] GameData { get; set; }

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

        private static void ProcessFile(string inputFile, string outputPath)
        {
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

                GameData? gameData = null;
                foreach (var game in GameData)
                {
                    if (game.ArchiveName.Equals(Path.GetFileNameWithoutExtension(inputFile), StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (game.Process.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
                        {
                            gameData = game;
                        }
                        break;
                    }
                } 

                if (gameData == null)
                {
                    Log($"Skipping '{Path.GetFileName(inputFile)}' as requested to skip in dataset.");
                    return;
                }

                Log($"Processing '{Path.GetFileName(inputFile)}'...");

                if (!Directory.Exists(unpackPath))
                {
                    Directory.CreateDirectory(unpackPath);
                }

                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                //var outpot = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(inputFile)}.xbe");
                //if (File.Exists(outpot) && new FileInfo(outpot).Length > 0)
                //{
                //    Log("Skipping as already processed.");
                //    return;
                //}

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

             //   var gameName = Path.GetFileNameWithoutExtension(inputFile);
            //    gameName = gameData.ISOName;

                Directory.CreateDirectory(Path.Combine(outputPath, gameData.XBETitleAndFolderName));

                using (var inputStream = new FileStream(input, FileMode.Open))
                using (var outputStream = new MemoryStream())
                {
                    var error = string.Empty;
                    if (XisoUtility.TryExtractDefaultFromXiso(inputStream, outputStream, ref error))
                    {
                        var attach = ResourceLoader.GetEmbeddedResourceBytes("attach.xbe");
                        var xbe = outputStream.ToArray();

                        if (XbeUtility.TryGetXbeImage(xbe, XbeUtility.ImageType.TitleImage, out var xprImage))
                        {
                            if (XprUtility.ConvertXprToPng(xprImage, out var pngImage))
                            {
                                if (!XbeUtility.TryReplaceXbeTitleImage(attach, pngImage))
                                {
                                    Log($"Error: failed to replace image");
                                    if (unpacked)
                                    {
                                        File.Delete(input);
                                    }
                                    return;
                                }
                            }
                            else
                            {
                                Log($"Error: failed to create png");
                                if (unpacked)
                                {
                                    File.Delete(input);
                                }
                                return;
                            }
                        }
                        else
                        {
                            Log($"Error: failed to extract xpr");
                            if (unpacked)
                            {
                                File.Delete(input);
                            }
                            return;
                        }
                                                
                        if (XbeUtility.ReplaceCertInfo(attach, xbe, gameData.XBETitleAndFolderName, out var patchedAttach))
                        {
                            File.WriteAllBytes(Path.Combine(outputPath, gameData.XBETitleAndFolderName, $"default.xbe"), patchedAttach);
                        }
                        else
                        {
                            Log($"Error: failed creating attach xbe");
                            if (unpacked)
                            {
                                File.Delete(input);
                            }
                            return;
                        }

                    }
                    else
                    {
                        Log($"Error: {error}");
                        if (unpacked)
                        {
                            File.Delete(input);
                        }
                        return;
                    }
                }

                Log("Remove Vdieo Partition & Split ISO...");
                XisoUtility.Split($"{input}", Path.Combine(outputPath, gameData.XBETitleAndFolderName), gameData.ISOName, true);

                if (unpacked)
                {
                    File.Delete(input);
                }

                File.Delete($"{input}.novideo");
            }
            catch (Exception ex)
            {
                Log($"Error Processing '{inputFile}' with error '{ex}'.");
            }
        }

        public static void StartConversion(string input, string output, string temp, string log)
        {
            FileStream? logStream = null;

            try
            {
                if (!string.IsNullOrEmpty(log))
                {
                    LogStream = File.OpenWrite(log);
                }

                var exePath = AppDomain.CurrentDomain.BaseDirectory;
                var repackList = Path.Combine(Path.GetDirectoryName(exePath), "RepackList.json");
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
                    ProcessFile(file, output);                    
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

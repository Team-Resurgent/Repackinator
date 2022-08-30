using System.Diagnostics;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Repackinator.Shared;
using Resurgent.UtilityBelt.Library.Utilities;
using Resurgent.UtilityBelt.Library.Utilities.XbeModels;
using SevenZipExtractor;

namespace Repackinator.Shared
{
    public class Repacker
    {
        private Action<string>? Logger { get; set; }

        private Action<ProgressInfo>? Progress { get; set; }

        private ProgressInfo CurrentProgress { get; set; } = new ProgressInfo();

        private string? TempFolder { get; set; }

        private GameData[]? GameDataList { get; set; }

        private void SendProgress()
        {
            if (Progress == null)
            {
                return;
            }
            Progress(CurrentProgress);
        }

        private void Log(string message)
        {            
            if (Logger == null)
            {
                return;
            }
            Logger(message);            
        }

        private void ProcessFile(string inputFile, string outputPath, GroupingEnum grouping, bool alternate, CancellationToken cancellationToken)
        {
            if (TempFolder == null)
            {
                Log($"Error: TempFolder should not be null.");
                return;
            }

            if (GameDataList == null)
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
                if (!extension.Equals(".iso") && !extension.Equals(".zip") && !extension.Equals(".7z") && !extension.Equals(".rar") && !extension.Equals(".iso"))
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
                    Log("Extracting ISO...");
                    try
                    {
                        using (ArchiveFile archiveFile = new ArchiveFile(inputFile))
                        {
                            foreach (Entry entry in archiveFile.Entries)
                            {
                                if (!Path.GetExtension(entry.FileName).Equals(".iso", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    continue;
                                }
                                input = Path.Combine(unpackPath, "unpacked.iso");
                                using (var fileStream = new FileStream(input, FileMode.Create))
                                {

                                    var extractProgress = new Action<float>((progress) =>
                                    {
                                        CurrentProgress.Progress2 = progress;
                                        CurrentProgress.Progress2Text = $"Extracting ISO...";
                                        SendProgress();
                                    });

                                    using (var progrtessStream = new ProgressStream(fileStream, (long)entry.Size, extractProgress))
                                    {
                                        entry.Extract(progrtessStream);
                                    }
                                }
                            }
                        }
                    } 
                    catch (Exception ex)
                    {
                        Log($"Error: failed to extract archive - {ex}");
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
                foreach (var game in GameDataList)
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

                if (gameData.XBETitleAndFolderNameAlt == null)
                {
                    Log($"Error: XBE title & folder name alt is null in dataset.");
                    return;
                }

                if (gameData.ISOName == null)
                {
                    Log($"Error: ISO name is null in dataset.");
                    return;
                }

                if (gameData.ISONameAlt == null)
                {
                    Log($"Error: ISO name alt is null in dataset.");
                    return;
                }

                if (gameData.Letter == null)
                {
                    Log($"Error: Letter is null in dataset.");
                    return;
                }

                if (grouping == GroupingEnum.Region)
                {
                    outputPath = Path.Combine(outputPath, gameData.Region);
                }
                else if (grouping == GroupingEnum.Letter)
                {
                    outputPath = Path.Combine(outputPath, gameData.Letter);
                }
                else if (grouping == GroupingEnum.RegionLetter)
                {
                    outputPath = Path.Combine(outputPath, gameData.Region, gameData.Letter);
                }
                else if (grouping == GroupingEnum.LetterRegion)
                {
                    outputPath = Path.Combine(outputPath, gameData.Letter, gameData.Region);
                }

                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                var xbeTitleAndFolderName = alternate ? gameData.XBETitleAndFolderNameAlt : gameData.XBETitleAndFolderName;
                var isoFileName = alternate ? gameData.ISONameAlt : gameData.ISOName;

                Directory.CreateDirectory(Path.Combine(outputPath, xbeTitleAndFolderName));

                var attach = ResourceLoader.GetEmbeddedResourceBytes("attach.xbe");
                if (XbeUtility.TryGetXbeImage(xbeData, XbeUtility.ImageType.TitleImage, out var xprImage))
                {
                    if (XprUtility.ConvertXprToJpeg(xprImage, out var jpgImage))
                    {
                        if (jpgImage != null)
                        {
                            File.WriteAllBytes(Path.Combine(outputPath, xbeTitleAndFolderName, "default.tbn"), jpgImage);
                        }
                        if (!XbeUtility.TryReplaceXbeTitleImage(attach, jpgImage))
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
                                                
                if (XbeUtility.ReplaceCertInfo(attach, xbeData, xbeTitleAndFolderName, out var patchedAttach) && patchedAttach != null)
                {
                    File.WriteAllBytes(Path.Combine(outputPath, xbeTitleAndFolderName, "default.xbe"), patchedAttach);
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

                var splitProgress = new Action<float>((progress) =>
                {
                    CurrentProgress.Progress2 = progress;
                    CurrentProgress.Progress2Text = $"Splitting ISO...";
                    SendProgress();
                });

                XisoUtility.Split($"{input}", Path.Combine(outputPath, xbeTitleAndFolderName), isoFileName, true, splitProgress, cancellationToken);

                CurrentProgress.Progress2 = 1.0f;
                SendProgress();

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

        public void StartConversion(Config config, Action<ProgressInfo>? progress, Action<string> logger, CancellationToken cancellationToken)
        {
            try
            {               
                Logger = logger;
                Progress = progress;

                GameDataList = GameDataHelper.LoadGameData();
                if (GameDataList == null)
                {
                    Log("Error: RepackList.json not found.");
                    return;
                }

                TempFolder = config.TempPath;              

                var files = Directory.GetFiles(config.InputPath);
                for (int i = 0; i < files.Length; i++)
                {
                    string? file = files[i];
                    CurrentProgress.Progress1 = i / (float)files.Length;
                    CurrentProgress.Progress1Text = $"Processing {i + 1} of {files.Length}";
                    SendProgress();

                    ProcessFile(file, config.OutputPath, config.Grouping, config.Alternative, cancellationToken);         
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
                CurrentProgress.Progress1 = 1.0f;                
                SendProgress();
            }
            catch (Exception ex)
            {
                logger($"Error: {ex}");
            }
        }
    }
}

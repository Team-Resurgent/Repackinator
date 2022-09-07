using Resurgent.UtilityBelt.Library.Utilities;
using Resurgent.UtilityBelt.Library.Utilities.XbeModels;
using SevenZipExtractor;

namespace Repackinator.Shared
{
    public class Repacker
    {
        private Action<LogMessage>? Logger { get; set; }

        private Action<ProgressInfo>? Progress { get; set; }

        private ProgressInfo CurrentProgress = new ProgressInfo();

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

        private void Log(LogMessageLevel level, string message)
        {            
            if (Logger == null)
            {
                return;
            }
            Logger(new LogMessage(level, message));            
        }

        private void ProcessFile(string inputFile, string outputPath, GroupingEnum grouping, bool alternate, CancellationToken cancellationToken)
        {
            if (TempFolder == null)
            {
                Log(LogMessageLevel.Error, "TempFolder should not be null.");
                return;
            }

            if (GameDataList == null)
            {
                Log(LogMessageLevel.Error, "GameData should not be null.");
                return;
            }

            var unpackPath = Path.Combine(TempFolder, "Unpack");
            var unpacked = false;
            var processInput = inputFile;
            var processOutput = string.Empty;

            try
            {
                if (!File.Exists(inputFile))
                {
                    Log(LogMessageLevel.Warning, $"Skipping '{Path.GetFileName(inputFile)}' as does not exist.");
                    return;
                }

                var extension = Path.GetExtension(inputFile).ToLower();
                if (!extension.Equals(".iso") && !extension.Equals(".zip") && !extension.Equals(".7z") && !extension.Equals(".rar") && !extension.Equals(".iso"))
                {
                    Log(LogMessageLevel.Warning, $"Skipping '{Path.GetFileName(inputFile)}' as unsupported extension."); 
                    return;
                }

                Log(LogMessageLevel.Info, $"Processing '{Path.GetFileName(inputFile)}'...");

                if (!Directory.Exists(unpackPath))
                {
                    Directory.CreateDirectory(unpackPath);
                }

                if (!extension.Equals(".iso"))
                {
                    Log(LogMessageLevel.Info, "Extracting ISO...");
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
                                processInput = Path.Combine(unpackPath, "unpacked.iso");
                                using (var fileStream = new FileStream(processInput, FileMode.Create))
                                {

                                    var extractProgress = new Action<float>((progress) =>
                                    {
                                        CurrentProgress.Progress2 = progress;
                                        CurrentProgress.Progress2Text = $"Extracting ISO...";
                                        SendProgress();
                                    });

                                    using (var progressStream = new ProgressStream(fileStream, (long)entry.Size, true, extractProgress))
                                    {
                                        entry.Extract(progressStream);
                                    }
                                }
                            }
                        }
                    } 
                    catch (Exception ex)
                    {
                        Log(LogMessageLevel.Error, $"Failed to extract archive - {ex}");
                        return;
                    }                    
                    unpacked = true;
                }

                var xbeData = Array.Empty<byte>();
                using (var inputStream = new FileStream(processInput, FileMode.Open))
                using (var outputStream = new MemoryStream())
                {
                    var error = string.Empty;
                    if (XisoUtility.TryExtractDefaultFromXiso(inputStream, outputStream, ref error))
                    {
                        xbeData = outputStream.ToArray();
                    }
                    else
                    {
                        Log(LogMessageLevel.Error, "Unable to extract default.xbe.");
                        return;
                    }
                }

                if (!XbeUtility.TryGetXbeCert(xbeData, out var cert) || cert == null)
                {
                    Log(LogMessageLevel.Error, $"Unable to get data from default.xbe.");
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
                        if (game.Process != null && game.Process.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
                        {
                            gameData = game;
                        }
                        break;
                    }
                } 

                if (!gameData.HasValue)
                {
                    if (found)
                    {
                        Log(LogMessageLevel.Warning, $"Skipping '{Path.GetFileName(inputFile)}' as requested to skip in dataset.");
                    }
                    else
                    {
                        Log(LogMessageLevel.Warning, $"Skipping '{Path.GetFileName(inputFile)}' as titleid, region and version not found in dataset.");
                    }
                    return;
                }

                if (gameData.Value.Region == null)
                {
                    Log(LogMessageLevel.Error, "Region is null in dataset.");
                    return;
                }

                if (gameData.Value.XBETitleAndFolderName == null)
                {
                    Log(LogMessageLevel.Error, "XBE title & folder name is null in dataset.");
                    return;
                }

                if (gameData.Value.XBETitleAndFolderNameAlt == null)
                {
                    Log(LogMessageLevel.Error, "XBE title & folder name alt is null in dataset.");
                    return;
                }

                if (gameData.Value.ISOName == null)
                {
                    Log(LogMessageLevel.Error, "ISO name is null in dataset.");
                    return;
                }

                if (gameData.Value.ISONameAlt == null)
                {
                    Log(LogMessageLevel.Error, "ISO name alt is null in dataset.");
                    return;
                }

                if (gameData.Value.Letter == null)
                {
                    Log(LogMessageLevel.Error, "Letter is null in dataset.");
                    return;
                }

                if (grouping == GroupingEnum.Region)
                {
                    outputPath = Path.Combine(outputPath, gameData.Value.Region);
                }
                else if (grouping == GroupingEnum.Letter)
                {
                    outputPath = Path.Combine(outputPath, gameData.Value.Letter);
                }
                else if (grouping == GroupingEnum.RegionLetter)
                {
                    outputPath = Path.Combine(outputPath, gameData.Value.Region, gameData.Value.Letter);
                }
                else if (grouping == GroupingEnum.LetterRegion)
                {
                    outputPath = Path.Combine(outputPath, gameData.Value.Letter, gameData.Value.Region);
                }

                processOutput = outputPath;

                var xbeTitleAndFolderName = alternate ? gameData.Value.XBETitleAndFolderNameAlt : gameData.Value.XBETitleAndFolderName;
                var isoFileName = alternate ? gameData.Value.ISONameAlt : gameData.Value.ISOName;

                Directory.CreateDirectory(Path.Combine(processOutput, xbeTitleAndFolderName));

                var attach = ResourceLoader.GetEmbeddedResourceBytes("attach.xbe");
                if (XbeUtility.TryGetXbeImage(xbeData, XbeUtility.ImageType.TitleImage, out var xprImage))
                {
                    if (XprUtility.ConvertXprToJpeg(xprImage, out var jpgImage))
                    {
                        if (jpgImage != null)
                        {
                            File.WriteAllBytes(Path.Combine(processOutput, xbeTitleAndFolderName, "default.tbn"), jpgImage);
                        }
                        if (!XbeUtility.TryReplaceXbeTitleImage(attach, jpgImage))
                        {
                            Log(LogMessageLevel.Error, "Failed to replace image.");
                            return;
                        }
                    }
                    else
                    {
                        Log(LogMessageLevel.Error, "Failed to create png.");
                        return;
                    }
                }
                else
                {
                    Log(LogMessageLevel.Error, "Failed to extract xpr.");
                    return;
                }
                                                
                if (XbeUtility.ReplaceCertInfo(attach, xbeData, xbeTitleAndFolderName, out var patchedAttach) && patchedAttach != null)
                {
                    File.WriteAllBytes(Path.Combine(processOutput, xbeTitleAndFolderName, "default.xbe"), patchedAttach);
                }
                else
                {
                    Log(LogMessageLevel.Error, "failed creating attach xbe.");
                    return;
                }

                Log(LogMessageLevel.Info, "Removing Video Partition & Splitting ISO...");

                var splitProgress = new Action<float>((progress) =>
                {
                    CurrentProgress.Progress2 = progress;
                    CurrentProgress.Progress2Text = $"Splitting ISO...";
                    SendProgress();
                });

                XisoUtility.Split($"{processInput}", Path.Combine(processOutput, xbeTitleAndFolderName), isoFileName, true, splitProgress, cancellationToken);

                CurrentProgress.Progress2 = 1.0f;
                SendProgress();

                Log(LogMessageLevel.Info, $"Completed Processing '{Path.GetFileName(inputFile)}'.");
            }
            catch (Exception ex)
            {
                Log(LogMessageLevel.Error, $"Processing '{inputFile}' caused error '{ex}'.");
            }
            finally
            {
                if (unpacked && File.Exists(processInput))
                {
                    File.Delete(processInput);
                }
                if (cancellationToken.IsCancellationRequested && Directory.Exists(processOutput))
                {
                    Directory.Delete(processOutput, true);
                }
            }
        }

        public void StartConversion(Config config, Action<ProgressInfo>? progress, Action<LogMessage> logger, CancellationToken cancellationToken)
        {
            try
            {               
                Logger = logger;
                Progress = progress;

                GameDataList = GameDataHelper.LoadGameData();
                if (GameDataList == null)
                {
                    Log(LogMessageLevel.Error, "RepackList.json not found.");
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

                Log(LogMessageLevel.Info, "Completed Processing List.");
            }
            catch (Exception ex)
            {
                Log(LogMessageLevel.Error, $"Exception occured '{ex}'.");
            }
        }
    }
}

using Resurgent.UtilityBelt.Library.Utilities;
using Resurgent.UtilityBelt.Library.Utilities.XbeModels;
using SevenZipExtractor;
using System.Diagnostics;
using System.Text;

namespace Repackinator.Shared
{
    public class Repacker
    {
        private Action<LogMessage>? Logger { get; set; }

        private Action<ProgressInfo>? Progress { get; set; }

        private ProgressInfo CurrentProgress = new ProgressInfo();

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
            var logMessage = new LogMessage(level, message);
            Logger(logMessage);
            var bytes = Encoding.UTF8.GetBytes(Utility.FormatLogMessage(logMessage));
            using var logStream = File.OpenWrite("RepackLog.txt");
            logStream.Write(bytes);
        }

        private void ProcessFile(string inputFile, string outputPath, GroupingEnum grouping, bool alternate, bool hasAllCrcs, Stopwatch procesTime, CancellationToken cancellationToken)
        {
            if (GameDataList == null)
            {
                Log(LogMessageLevel.Error, "GameData should not be null.");
                return;
            }

            var processStopwatch = new Stopwatch();
            processStopwatch.Start();

            var unpackPath = Path.Combine(outputPath, "Repackinator-Temp");

            if (Directory.Exists(unpackPath))
            {
                Directory.Delete(unpackPath, true);
            }
            Directory.CreateDirectory(unpackPath);

            var processOutput = string.Empty;
            var deleteProcessOutput = false;

            try
            {
                if (!File.Exists(inputFile))
                {
                    Log(LogMessageLevel.NotFound, $"Not found as '{Path.GetFileName(inputFile)}' does not exist.");
                    return;
                }

                var extension = Path.GetExtension(inputFile).ToLower();
                if (!extension.Equals(".iso") && !extension.Equals(".zip") && !extension.Equals(".7z") && !extension.Equals(".rar") && !extension.Equals(".iso"))
                {
                    Log(LogMessageLevel.Warning, $"File '{Path.GetFileName(inputFile)}' has an unsupported extension.");
                    return;
                }

                Log(LogMessageLevel.Info, $"Processing '{Path.GetFileName(inputFile)}'...");

                if (!extension.Equals(".iso"))
                {
                    Log(LogMessageLevel.Info, "Extracting, Removing Video Partition & Splitting ISO...");
                    try
                    {
                        using (ArchiveFile archiveFile = new ArchiveFile(inputFile, cancellationToken))
                        {
                            foreach (Entry entry in archiveFile.Entries)
                            {
                                if (!Path.GetExtension(entry.FileName).Equals(".iso", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    continue;
                                }

                                var entryCRC = entry.CRC.ToString("X8");

                                bool inDatasetZip = false;
                                bool processArchive = !hasAllCrcs;
                                foreach (var game in GameDataList)
                                {
                                    if (game.ISOChecksum.PadLeft(8, '0').Equals(entryCRC, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        inDatasetZip = true;
                                        processArchive = game.Process.Equals("Y", StringComparison.CurrentCultureIgnoreCase);
                                        break;
                                    }
                                }

                                if (processArchive)
                                {
                                    using (var fileStream1 = new FileStream(Path.Combine(unpackPath, @"Repackinator.1.iso"), FileMode.Create))
                                    using (var fileStream2 = new FileStream(Path.Combine(unpackPath, @"Repackinator.2.iso"), FileMode.Create))
                                    {
                                        var extractProgress = new Action<float>((progress) =>
                                        {
                                            CurrentProgress.Progress2 = progress;
                                            CurrentProgress.Progress2Text = $"Extracting, Removing Video Partition & Splitting ISO...";
                                            SendProgress();
                                        });
                                        using (var extractSplitStream = new ExtractSplitStream(fileStream1, fileStream2, (long)entry.Size, extractProgress))
                                        {
                                            entry.Extract(extractSplitStream, cancellationToken);
                                        }
                                    }
                                }
                                else
                                {
                                    if (inDatasetZip)
                                    {
                                        Log(LogMessageLevel.Skipped, $"Skipping '{Path.GetFileName(inputFile)}' as requested to skip in dataset based on user selection.");
                                    }
                                    else
                                    {
                                        Log(LogMessageLevel.NotFound, $"Not found info for '{Path.GetFileName(inputFile)}' as CRC not found in dataset.");
                                    }
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(LogMessageLevel.Error, $"Failed to extract archive - {ex}");
                        return;
                    }
                }
                else
                {
                    Log(LogMessageLevel.Info, "Removing Video Partition & Splitting ISO...");

                    var splitProgress = new Action<float>((progress) =>
                    {
                        CurrentProgress.Progress2 = progress;
                        CurrentProgress.Progress2Text = $"Removing Video Partition & Splitting ISO...";
                        SendProgress();
                    });

                    XisoUtility.Split(inputFile, unpackPath, "Repackinator", true, splitProgress, cancellationToken);

                    CurrentProgress.Progress2 = 1.0f;
                    SendProgress();
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var xbeData = Array.Empty<byte>();
                using (var inputStream1 = new FileStream(Path.Combine(unpackPath, @"Repackinator.1.iso"), FileMode.Open))
                using (var inputStream2 = new FileStream(Path.Combine(unpackPath, @"Repackinator.2.iso"), FileMode.Open))
                using (var outputStream = new MemoryStream())
                {
                    var error = string.Empty;
                    if (XisoUtility.TryExtractDefaultFromSplitXiso(inputStream1, inputStream2, outputStream, ref error))
                    {
                        xbeData = outputStream.ToArray();
                    }
                    else
                    {
                        Log(LogMessageLevel.Error, $"Unable to extract default.xbe due to '{error}'.");
                        return;
                    }
                }

                if (!XbeUtility.TryGetXbeCert(xbeData, out var cert) || cert == null)
                {
                    Log(LogMessageLevel.Error, $"Unable to get data from default.xbe.");
                    return;
                }

                var titleId = cert.Value.Title_Id.ToString("X8");
                var gameRegion = XbeCertificate.GameRegionToString(cert.Value.Game_Region);
                var version = cert.Value.Version.ToString("X8");

                bool inDatasetISO = false;

                GameData? gameData = null;
                foreach (var game in GameDataList)
                {
                    if (game.TitleID == titleId && game.Region == gameRegion && game.Version == version)
                    {
                        inDatasetISO = true;
                        if (game.Process != null && game.Process.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
                        {
                            gameData = game;
                        }
                        break;
                    }
                }

                if (!gameData.HasValue)
                {
                    if (inDatasetISO)
                    {
                        Log(LogMessageLevel.Skipped, $"Skipping '{Path.GetFileName(inputFile)}' as requested to skip in dataset based on xbe info.");
                    }
                    else
                    {
                        Log(LogMessageLevel.NotFound, $"Not found info for '{Path.GetFileName(inputFile)}' as titleid, region and version not found in dataset.");
                    }
                    return;
                }

                if (gameData.Value.Region == null)
                {
                    Log(LogMessageLevel.Error, "Region is null in dataset.");
                    return;
                }

                if (gameData.Value.XBETitle == null)
                {
                    Log(LogMessageLevel.Error, "XBE title is null in dataset.");
                    return;
                }

                if (gameData.Value.XBETitleAlt == null)
                {
                    Log(LogMessageLevel.Error, "XBE title alt is null in dataset.");
                    return;
                }

                if (gameData.Value.FolderName == null)
                {
                    Log(LogMessageLevel.Error, "Folder name is null in dataset.");
                    return;
                }

                if (gameData.Value.FolderNameAlt == null)
                {
                    Log(LogMessageLevel.Error, "Folder name alt is null in dataset.");
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

                var xbeTitle = alternate ? gameData.Value.XBETitleAlt : gameData.Value.XBETitle;
                var folderName = alternate ? gameData.Value.FolderNameAlt : gameData.Value.FolderName;
                var isoFileName = alternate ? gameData.Value.ISONameAlt : gameData.Value.ISOName;

                processOutput = Path.Combine(outputPath, folderName);

                if (Directory.Exists(processOutput))
                {
                    Directory.Delete(processOutput, true);
                }
                Directory.CreateDirectory(processOutput);

                File.Move(Path.Combine(unpackPath, @"Repackinator.1.iso"), Path.Combine(processOutput, $"{isoFileName}.1.iso"));
                File.Move(Path.Combine(unpackPath, @"Repackinator.2.iso"), Path.Combine(processOutput, $"{isoFileName}.2.iso"));

                var attach = ResourceLoader.GetEmbeddedResourceBytes("attach.xbe");
                if (XbeUtility.TryGetXbeImage(xbeData, XbeUtility.ImageType.TitleImage, out var xprImage))
                {
                    if (XprUtility.ConvertXprToJpeg(xprImage, out var jpgImage))
                    {
                        if (jpgImage != null)
                        {
                            File.WriteAllBytes(Path.Combine(processOutput, "default.tbn"), jpgImage);
                        }
                        if (!XbeUtility.TryReplaceXbeTitleImage(attach, jpgImage))
                        {
                            deleteProcessOutput = true;
                            Log(LogMessageLevel.Error, "Failed to replace image.");
                            return;
                        }
                    }
                    else
                    {
                        deleteProcessOutput = true;
                        Log(LogMessageLevel.Error, "Failed to create png.");
                        return;
                    }
                }
                else
                {
                    Log(LogMessageLevel.Warning, "Failed to extract xpr as probably missing, will use default image.");
                }

                if (XbeUtility.ReplaceCertInfo(attach, xbeData, xbeTitle, out var patchedAttach) && patchedAttach != null)
                {
                    File.WriteAllBytes(Path.Combine(processOutput, "default.xbe"), patchedAttach);
                }
                else
                {
                    deleteProcessOutput = true;
                    Log(LogMessageLevel.Error, "failed creating attach xbe.");
                    return;
                }

                processStopwatch.Stop();
                Log(LogMessageLevel.Completed, $"Completed Processing '{Path.GetFileName(inputFile)}' (Time Taken {processStopwatch.Elapsed.Hours:00}:{processStopwatch.Elapsed.Minutes:00}:{processStopwatch.Elapsed.Seconds:00}).");
            }
            catch (Exception ex)
            {
                deleteProcessOutput = true;
                Log(LogMessageLevel.Error, $"Processing '{inputFile}' caused error '{ex}'.");
            }
            finally
            {
                if (deleteProcessOutput && Directory.Exists(processOutput))
                {
                    Directory.Delete(processOutput, true);
                }
                if (Directory.Exists(unpackPath))
                {
                    Directory.Delete(unpackPath, true);
                }
                if (cancellationToken.IsCancellationRequested && Directory.Exists(processOutput))
                {
                    Directory.Delete(processOutput, true);
                }
            }
        }

        public void StartRepacking(GameData[]? gameData, Config config, Action<ProgressInfo>? progress, Action<LogMessage> logger, Stopwatch stopwatch, CancellationToken cancellationToken)
        {
            try
            {
                Logger = logger;
                Progress = progress;

                GameDataList = gameData;
                if (GameDataList == null)
                {
                    Log(LogMessageLevel.Error, "RepackList.json not found.");
                    return;
                }

                int crcMissingCount = 0;
                foreach (var gameDataItem in GameDataList)
                {
                    if (string.IsNullOrEmpty(gameDataItem.ISOChecksum))
                    {
                        crcMissingCount++;                        
                    }
                }

                stopwatch.Restart();

                if (File.Exists("RepackLog.txt"))
                {
                    File.Delete("RepackLog.txt");
                }

                if (crcMissingCount > 0)
                {
                    Log(LogMessageLevel.Warning, $"There are {crcMissingCount} ISO CRC's missing this will cause compressed ISO's to take a while longer.");
                    Log(LogMessageLevel.None, "");
                }

                CurrentProgress.Progress1 = 0;
                CurrentProgress.Progress1Text = string.Empty;
                CurrentProgress.Progress2 = 0;
                CurrentProgress.Progress2Text = string.Empty;
                SendProgress();

                var files = Directory.GetFiles(config.InputPath);
                for (int i = 0; i < files.Length; i++)
                {
                    string? file = files[i];
                    CurrentProgress.Progress1 = i / (float)files.Length;
                    CurrentProgress.Progress1Text = $"Processing {i + 1} of {files.Length}";
                    SendProgress();

                    ProcessFile(file, config.OutputPath, config.Grouping, config.Alternative, crcMissingCount == 0, stopwatch, cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        Log(LogMessageLevel.None, "");
                        Log(LogMessageLevel.Info, "Cancelled.");
                        break;
                    }

                    Log(LogMessageLevel.None, "");

                }

                CurrentProgress.Progress1 = 1.0f;
                SendProgress();

                stopwatch.Stop();

                Log(LogMessageLevel.Done, $"Completed Processing List (Time Taken {stopwatch.Elapsed.Hours:00}:{stopwatch.Elapsed.Minutes:00}:{stopwatch.Elapsed.Seconds:00}).");
            }
            catch (Exception ex)
            {
                Log(LogMessageLevel.Error, $"Exception occured '{ex}'.");
            }
        }
    }
}

using Downloader;
using Repackinator.Exceptions;
using Repackinator.Helpers;
using Repackinator.Logging;
using Repackinator.Models;
using Repackinator.Streams;
using Resurgent.UtilityBelt.Library.Utilities;
using Resurgent.UtilityBelt.Library.Utilities.ImageInput;
using Resurgent.UtilityBelt.Library.Utilities.XbeModels;
using SharpCompress.Archives;
using System.Diagnostics;
using System.Text;

namespace Repackinator.Actions
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
            File.AppendAllText("RepackLog.txt", logMessage.ToLogFormat());
        }

        private int ProcessFile(string inputFile, string outputPath, GroupingEnum grouping, bool hasAllCrcs, bool upperCase, bool compress, bool trimmedScrub, CancellationToken cancellationToken)
        {
            try
            {
                if (!File.Exists(inputFile))
                {
                    Log(LogMessageLevel.NotFound, $"Not found as '{Path.GetFileName(inputFile)}' does not exist.");
                    return -1;
                }

                Log(LogMessageLevel.Info, $"Processing '{Path.GetFileName(inputFile)}'...");

                var extension = Path.GetExtension(inputFile).ToLower();
                if (extension.Equals(".iso") || extension.Equals(".cso") || extension.Equals(".cci"))
                {
                    return ProcessIso(inputFile, outputPath, grouping, upperCase, compress, trimmedScrub, cancellationToken);
                }

                return ProcessArchive(inputFile, outputPath, grouping, hasAllCrcs, upperCase, compress, trimmedScrub, cancellationToken);
            }
            catch (Exception ex)
            {
                Log(LogMessageLevel.Error, $"Processing '{inputFile}' caused error '{ex}'.");
                return -1;
            }
        }

        public int ProcessArchive(string inputFile, string outputPath, GroupingEnum grouping, bool hasAllCrcs, bool upperCase, bool compress, bool trimmedScrub, CancellationToken cancellationToken)
        {
            if (GameDataList == null)
            {
                Log(LogMessageLevel.Error, "GameData should not be null.");
                return -1;
            }

            var unpackPath = Path.Combine(outputPath, "Repackinator-Temp");
            var processOutput = string.Empty;
            var deleteProcessOutput = false;

            try
            {
                var processStopwatch = new Stopwatch();
                processStopwatch.Start();

                if (Directory.Exists(unpackPath))
                {
                    try
                    {
                        Directory.Delete(unpackPath, true);
                    }
                    catch (IOException)
                    {
                        Log(LogMessageLevel.Error, $"Failed to delete directory '{unpackPath}, close any windows accessing the folder.");
                        return -1;
                    }
                }
                Directory.CreateDirectory(unpackPath);

                var needsSecondPass = false;

                var isoFound = false;

                try
                {
                    using (var archiveStream = File.OpenRead(inputFile))
                    using (var archive = ArchiveFactory.Open(archiveStream))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (!Path.GetExtension(entry.Key).Equals(".iso", StringComparison.CurrentCultureIgnoreCase))
                            {
                                continue;
                            }

                            isoFound = true;

                            var entryCRC = entry.Crc.ToString("X8");

                            bool processArchive = !hasAllCrcs;

                            GameData? tempGameData = null;
                            for (var i = 0; i < GameDataList.Length; i++)
                            {
                                GameData game = GameDataList[i];
                                if (game.ISOChecksum.PadLeft(8, '0').Equals(entryCRC, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    processArchive = game.Process.Equals("Y", StringComparison.CurrentCultureIgnoreCase);
                                    tempGameData = game;
                                    break;
                                }
                            }

                            if (processArchive)
                            {
                                Log(LogMessageLevel.Info, "Extracting And Splitting ISO...");

                                var willScrub = tempGameData == null ? false : tempGameData.Value.Scrub.Equals("Y", StringComparison.CurrentCultureIgnoreCase);
                                if (tempGameData == null || compress == true || willScrub == true)
                                {
                                    needsSecondPass = true;
                                }

                                using (var fileStream1 = new FileStream(Path.Combine(unpackPath, @"Repackinator.1.temp"), FileMode.Create))
                                using (var fileStream2 = new FileStream(Path.Combine(unpackPath, @"Repackinator.2.temp"), FileMode.Create))
                                {
                                    var extractProgress = new Action<float>((progress) =>
                                    {
                                        CurrentProgress.Progress2 = progress;
                                        CurrentProgress.Progress2Text = $"Extracting And Splitting ISO...";
                                        SendProgress();
                                    });
                                    using (var extractSplitStream = new ExtractSplitStream(fileStream1, fileStream2, entry.Size, extractProgress, cancellationToken))
                                    {
                                        entry.WriteTo(extractSplitStream);
                                    }
                                }

                            }
                            else
                            {
                                if (tempGameData != null)
                                {
                                    Log(LogMessageLevel.Skipped, $"Skipping '{Path.GetFileName(inputFile)}' as requested to skip in dataset based on user selection.");
                                }
                                else
                                {
                                    Log(LogMessageLevel.NotFound, $"Not found info for '{Path.GetFileName(inputFile)}' as CRC not found in dataset.");
                                }
                                return -1;
                            }
                        }
                    }
                }
                catch (ExtractAbortException)
                {
                    return -1;
                }
                catch (Exception ex)
                {
                    Log(LogMessageLevel.Error, $"Failed to extract archive - {ex}");
                    return -1;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return -1;
                }

                if (!isoFound)
                {
                    Log(LogMessageLevel.Error, $"Repackinator only accepts archive containing an ISO file.");
                    return -1;
                }

                var xbeData = Array.Empty<byte>();
                using (var xisoInput = new XisoInput(new string[] { Path.Combine(unpackPath, @"Repackinator.1.temp"), Path.Combine(unpackPath, @"Repackinator.2.temp") }))
                {
                    if (!XisoUtility.TryGetDefaultXbeFromXiso(xisoInput, ref xbeData))
                    {
                        Log(LogMessageLevel.Error, $"Unable to extract default.xbe.");
                        return -1;
                    }
                }

                if (!XbeUtility.TryGetXbeCert(xbeData, out var cert) || cert == null)
                {
                    Log(LogMessageLevel.Error, $"Unable to get data from default.xbe.");
                    return -1;
                }

                var titleId = cert.Title_Id.ToString("X8");
                var gameRegion = XbeCertificate.GameRegionToString(cert.Game_Region);
                var version = cert.Version.ToString("X8");

                bool inDatasetISO = false;
                int gameIndex = -1;
                GameData? gameData = null;
                for (var i = 0; i < GameDataList.Length; i++)
                {
                    GameData game = GameDataList[i];
                    if (game.TitleID == titleId && game.Region == gameRegion && game.Version == version)
                    {
                        inDatasetISO = true;
                        if (game.Process != null && game.Process.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
                        {
                            gameData = game;
                            gameIndex = i;
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
                    return -1;
                }

                if (gameData.Value.Region == null)
                {
                    Log(LogMessageLevel.Error, "Region is null in dataset.");
                    return -1;
                }

                if (gameData.Value.XBETitle == null)
                {
                    Log(LogMessageLevel.Error, "XBE title is null in dataset.");
                    return -1;
                }

                if (gameData.Value.FolderName == null)
                {
                    Log(LogMessageLevel.Error, "Folder name is null in dataset.");
                    return -1;
                }

                if (gameData.Value.ISOName == null)
                {
                    Log(LogMessageLevel.Error, "ISO name is null in dataset.");
                    return -1;
                }

                if (gameData.Value.Letter == null)
                {
                    Log(LogMessageLevel.Error, "Letter is null in dataset.");
                    return -1;
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

                var xbeTitle = upperCase ? gameData.Value.XBETitle.ToUpper() : gameData.Value.XBETitle;
                var folderName = upperCase ? gameData.Value.FolderName.ToUpper() : gameData.Value.FolderName;
                var isoFileName = upperCase ? gameData.Value.ISOName.ToUpper() : gameData.Value.ISOName;
                var scrub = gameData.Value.Scrub != null && gameData.Value.Scrub.Equals("Y", StringComparison.CurrentCultureIgnoreCase);

                processOutput = Path.Combine(outputPath, folderName);

                if (Directory.Exists(processOutput))
                {
                    try
                    {
                        Directory.Delete(processOutput, true);
                    }
                    catch (IOException)
                    {
                        Log(LogMessageLevel.Error, $"Failed to delete directory '{processOutput}, close any windows accessing the folder.");
                        return -1;
                    }
                }
                Directory.CreateDirectory(processOutput);

                if (needsSecondPass)
                {
                    if (compress == true)
                    {
                        var message = $"Creating Compressed ISO...";

                        Log(LogMessageLevel.Info, message);

                        var repackProgress = new Action<int, float>((stage, progress) =>
                        {
                            CurrentProgress.Progress2 = progress;

                            if (stage == 0)
                            {
                                CurrentProgress.Progress2Text = "Processing Data Sectors...";
                            }
                            else if (stage == 1)
                            {
                                CurrentProgress.Progress2Text = "Processing Security Sectors...";
                            }
                            else 
                            {
                                CurrentProgress.Progress2Text = message;
                            }

                            SendProgress();
                        });

                        using (var isoInput = new XisoInput(new string[] { Path.Combine(unpackPath, @"Repackinator.1.temp"), Path.Combine(unpackPath, @"Repackinator.2.temp") }))
                        {
                            if (!XisoUtility.CreateCCI(isoInput, processOutput, isoFileName, ".cci", scrub, trimmedScrub, repackProgress, cancellationToken))
                            {
                                Log(LogMessageLevel.Error, $"Unable process file 'Repackinator.temp'.");
                                return -1;
                            }
                        }
                    }
                    else
                    {
                        var message = $"Creating ISO...";

                        Log(LogMessageLevel.Info, message);

                        var repackProgress = new Action<int, float>((stage, progress) =>
                        {
                            CurrentProgress.Progress2 = progress;

                            if (stage == 0)
                            {
                                CurrentProgress.Progress2Text = "Processing Data Sectors...";
                            }
                            else if (stage == 1)
                            {
                                CurrentProgress.Progress2Text = "Processing Security Sectors...";
                            }
                            else
                            {
                                CurrentProgress.Progress2Text = message;
                            }

                            SendProgress();
                        });

                        using (var isoInput = new XisoInput(new string[] { Path.Combine(unpackPath, @"Repackinator.1.temp"), Path.Combine(unpackPath, @"Repackinator.2.temp") }))
                        {
                            if (!XisoUtility.Split(isoInput, processOutput, isoFileName, ".iso", scrub, trimmedScrub, repackProgress, cancellationToken))
                            {
                                Log(LogMessageLevel.Error, $"Unable process file 'Repackinator.temp'.");
                                return -1;
                            }
                        }
                    }

                    CurrentProgress.Progress2 = 1.0f;
                    SendProgress();

                    File.Delete(Path.Combine(unpackPath, @"Repackinator.1.temp"));
                    File.Delete(Path.Combine(unpackPath, @"Repackinator.2.temp"));
                }
                else
                {
                    File.Move(Path.Combine(unpackPath, @"Repackinator.1.temp"), Path.Combine(processOutput, $"{isoFileName}.1.iso"));
                    File.Move(Path.Combine(unpackPath, @"Repackinator.2.temp"), Path.Combine(processOutput, $"{isoFileName}.2.iso"));
                }

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
                            return -1;
                        }
                    }
                    else
                    {
                        deleteProcessOutput = true;
                        Log(LogMessageLevel.Error, "Failed to create jpg.");
                        return -1;
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
                    return -1;
                }

                processStopwatch.Stop();
                Log(LogMessageLevel.Completed, $"Completed Processing '{Path.GetFileName(inputFile)}' (Time Taken {processStopwatch.Elapsed.TotalHours:00}:{processStopwatch.Elapsed.Minutes:00}:{processStopwatch.Elapsed.Seconds:00}).");
                return gameIndex;
            }
            catch (Exception ex)
            {
                deleteProcessOutput = true;
                Log(LogMessageLevel.Error, $"Processing '{inputFile}' caused error '{ex}'.");
                return -1;
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

        public int ProcessIso(string inputFile, string outputPath, GroupingEnum grouping, bool upperCase, bool compress, bool trimmedScrub, CancellationToken cancellationToken)
        {
            if (GameDataList == null)
            {
                Log(LogMessageLevel.Error, "GameData should not be null.");
                return -1;
            }

            var processOutput = string.Empty;
            var deleteProcessOutput = false;

            var inputSlices = Utility.GetSlicesFromFile(inputFile);

            try
            {
                var processStopwatch = new Stopwatch();
                processStopwatch.Start();

                var xbeData = Array.Empty<byte>();
                using (var xisoInput = ImageImputHelper.GetImageInput(inputSlices))
                {
                    if (!XisoUtility.TryGetDefaultXbeFromXiso(xisoInput, ref xbeData))
                    {
                        Log(LogMessageLevel.Error, $"Unable to extract default.xbe.");
                        return -1;
                    }
                }

                if (!XbeUtility.TryGetXbeCert(xbeData, out var cert) || cert == null)
                {
                    Log(LogMessageLevel.Error, $"Unable to get data from default.xbe.");
                    return -1;
                }

                var titleId = cert.Title_Id.ToString("X8");
                var gameRegion = XbeCertificate.GameRegionToString(cert.Game_Region);
                var version = cert.Version.ToString("X8");

                bool inDatasetISO = false;

                int gameIndex = -1;
                GameData? gameData = null;
                for (var i = 0; i < GameDataList.Length; i++)
                {
                    GameData game = GameDataList[i];
                    if (game.TitleID == titleId && game.Region == gameRegion && game.Version == version)
                    {
                        inDatasetISO = true;
                        if (game.Process != null && game.Process.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
                        {
                            gameData = game;
                            gameIndex = i;
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
                    return -1;
                }

                if (gameData.Value.Region == null)
                {
                    Log(LogMessageLevel.Error, "Region is null in dataset.");
                    return -1;
                }

                if (gameData.Value.XBETitle == null)
                {
                    Log(LogMessageLevel.Error, "XBE title is null in dataset.");
                    return -1;
                }

                if (gameData.Value.FolderName == null)
                {
                    Log(LogMessageLevel.Error, "Folder name is null in dataset.");
                    return -1;
                }

                if (gameData.Value.ISOName == null)
                {
                    Log(LogMessageLevel.Error, "ISO name is null in dataset.");
                    return -1;
                }

                if (gameData.Value.Letter == null)
                {
                    Log(LogMessageLevel.Error, "Letter is null in dataset.");
                    return -1;
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

                var xbeTitle = upperCase ? gameData.Value.XBETitle.ToUpper() : gameData.Value.XBETitle;
                var folderName = upperCase ? gameData.Value.FolderName.ToUpper() : gameData.Value.FolderName;
                var isoFileName = upperCase ? gameData.Value.ISOName.ToUpper() : gameData.Value.ISOName;
                var scrub = gameData.Value.Scrub != null && gameData.Value.Scrub.Equals("Y", StringComparison.CurrentCultureIgnoreCase);

                processOutput = Path.Combine(outputPath, folderName);

                if (Directory.Exists(processOutput))
                {
                    try
                    {
                        Directory.Delete(processOutput, true);
                    }
                    catch (IOException)
                    {
                        Log(LogMessageLevel.Error, $"Failed to delete directory '{processOutput}, close any windows accessing the folder.");
                        return -1;
                    }
                }
                Directory.CreateDirectory(processOutput);

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
                            return -1;
                        }
                    }
                    else
                    {
                        deleteProcessOutput = true;
                        Log(LogMessageLevel.Error, "Failed to create jpg.");
                        return -1;
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
                    return -1;
                }

                if (compress == true)
                {
                    var message = $"Creating Compressed ISO...";

                    Log(LogMessageLevel.Info, message);

                    var repackProgress = new Action<int, float>((stage, progress) =>
                    {
                        CurrentProgress.Progress2 = progress;

                        if (stage == 0)
                        {
                            CurrentProgress.Progress2Text = "Processing Data Sectors...";
                        }
                        else if (stage == 1)
                        {
                            CurrentProgress.Progress2Text = "Processing Security Sectors...";
                        }
                        else
                        {
                            CurrentProgress.Progress2Text = message;
                        }

                        SendProgress();
                    });

                    using (var cciInput = ImageImputHelper.GetImageInput(inputSlices))
                    {
                        if (!XisoUtility.CreateCCI(cciInput, processOutput, isoFileName, ".cci", scrub, trimmedScrub, repackProgress, cancellationToken))
                        {
                            Log(LogMessageLevel.Error, $"Unable process file '{inputFile}'.");
                            return -1;
                        }
                    }
                }
                else
                {
                    var message = $"Creating ISO...";

                    Log(LogMessageLevel.Info, message);

                    var repackProgress = new Action<int, float>((stage, progress) =>
                    {
                        CurrentProgress.Progress2 = progress;

                        if (stage == 0)
                        {
                            CurrentProgress.Progress2Text = "Processing Data Sectors...";
                        }
                        else if (stage == 1)
                        {
                            CurrentProgress.Progress2Text = "Processing Security Sectors...";
                        }
                        else
                        {
                            CurrentProgress.Progress2Text = message;
                        }

                        SendProgress();
                    });

                    using (var isoInput = ImageImputHelper.GetImageInput(inputSlices))
                    {
                        if (!XisoUtility.Split(isoInput, processOutput, isoFileName, ".iso", scrub, trimmedScrub, repackProgress, cancellationToken))
                        {
                            Log(LogMessageLevel.Error, $"Unable process file '{inputFile}'.");
                            return -1;
                        }
                    }
                }

                CurrentProgress.Progress2 = 1.0f;
                SendProgress();

                if (cancellationToken.IsCancellationRequested)
                {
                    deleteProcessOutput = true;
                    return -1;
                }

                processStopwatch.Stop();
                Log(LogMessageLevel.Completed, $"Completed Processing '{Path.GetFileName(inputFile)}' (Time Taken {processStopwatch.Elapsed.TotalHours:00}:{processStopwatch.Elapsed.Minutes:00}:{processStopwatch.Elapsed.Seconds:00}).");
                return gameIndex;
            }
            catch (Exception ex)
            {
                deleteProcessOutput = true;
                Log(LogMessageLevel.Error, $"Processing '{inputFile}' caused error '{ex}'.");
                return -1;
            }
            finally
            {
                if (deleteProcessOutput && Directory.Exists(processOutput))
                {
                    Directory.Delete(processOutput, true);
                }
                if (cancellationToken.IsCancellationRequested && Directory.Exists(processOutput))
                {
                    Directory.Delete(processOutput, true);
                }
            }
        }

        private async Task<bool> DownloadFromUrlToPath(string url, string path, Action<long, long, double> downloaded, CancellationToken cancellationToken)
        {
            var downloadOpt = new DownloadConfiguration()
            {
                ChunkCount = 8,
                ParallelDownload = true,
                MaxTryAgainOnFailover = 5,
                ReserveStorageSpaceBeforeStartingDownload = true
            };

            var downloader = new DownloadService(downloadOpt);
            downloader.DownloadProgressChanged += (s, e) =>
            {
                downloaded(e.ReceivedBytesSize, e.TotalBytesToReceive, e.BytesPerSecondSpeed);
                if (cancellationToken.IsCancellationRequested)
                {
                    downloader.CancelAsync();
                }
            };

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            await downloader.DownloadFileTaskAsync(url, path);

            return downloader.Status == DownloadStatus.Completed;
        }

        public void StartRepacking(GameData[]? gameData, Config config, Action<ProgressInfo>? progress, Action<LogMessage> logger, Stopwatch stopwatch, CancellationToken cancellationToken)
        {
            try
            {
                Logger = logger;
                Progress = progress;

                if (gameData == null)
                {
                    Log(LogMessageLevel.Error, "RepackList.json not found.");
                    return;
                }

                GameDataList = gameData;

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

                if (config.LeechType > 0)
                {
                    var count = 0;
                    var leechlistCount = GameDataList.Where(l => l.Process.Equals("Y", StringComparison.CurrentCultureIgnoreCase)).Count();

                    for (int i = 0; i < GameDataList.Length; i++)
                    {
                        GameData gameDataItem = GameDataList[i];
                        if (!gameDataItem.Process.Equals("Y", StringComparison.CurrentCultureIgnoreCase))
                        {
                            continue;
                        }

                        count++;

                        var decodedLink = gameDataItem.Link;
                        if (!decodedLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
                        {
                            byte[] linkBytes = Convert.FromBase64String(gameDataItem.Link);
                            decodedLink = Encoding.ASCII.GetString(linkBytes);
                        }

                        var tempPath = Path.Combine(Path.GetTempPath(), $"RepackinatorDownload{Path.GetExtension(decodedLink)}");
                        if (config.LeechType > 1)
                        {
                            tempPath = Path.Combine(config.InputPath, $"{gameDataItem.ISOName}{Path.GetExtension(decodedLink)}");
                        }

                        if (File.Exists(tempPath))
                        {
                            File.Delete(tempPath);
                        }

                        try
                        {
                            CurrentProgress.Progress1 = i / (float)GameDataList.Length;
                            CurrentProgress.Progress1Text = $"Processing {count} of {leechlistCount}";
                            SendProgress();

                            Log(LogMessageLevel.Info, $"Downloading '{gameDataItem.ISOName}'.");

                            var result = DownloadFromUrlToPath(decodedLink, tempPath, (downloaded, totalLength, bytesPerSecond) =>
                            {
                                CurrentProgress.Progress2 = downloaded / (float)totalLength;
                                CurrentProgress.Progress2Text = $"Downloaded {Math.Round(downloaded / (1024 * 1024.0f), 2)}MB of {Math.Round(totalLength / (1024 * 1024.0f), 2)}MB ({Math.Round(bytesPerSecond / 1024, 2)}KB/s)";
                                SendProgress();
                            }, cancellationToken).Result;

                            if (cancellationToken.IsCancellationRequested)
                            {
                                Log(LogMessageLevel.None, "");
                                Log(LogMessageLevel.Info, "Cancelled.");
                                break;
                            }

                            if (!result)
                            {
                                Log(LogMessageLevel.Error, "Download Failed.");
                                continue;
                            }

                            if (config.LeechType == 3)
                            {
                                gameDataItem.Process = "N";
                                GameDataHelper.SaveGameData(gameData);
                                continue;
                            }

                            var gameIndex = ProcessFile(tempPath, config.OutputPath, config.Grouping, crcMissingCount == 0, config.UpperCase, config.Compress, config.TrimmedScrub, cancellationToken);
                            if (gameIndex >= 0)
                            {
                                gameData[gameIndex].Process = "N";
                                GameDataHelper.SaveGameData(gameData);
                            }
                        }
                        catch
                        {
                            throw;
                        }
                        finally
                        {
                            if (config.LeechType == 1 && File.Exists(tempPath))
                            {
                                File.Delete(tempPath);                                
                            }
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            Log(LogMessageLevel.None, "");
                            Log(LogMessageLevel.Info, "Cancelled.");
                            break;
                        }

                        Log(LogMessageLevel.None, "");
                    }
                }
                else
                {
                    var acceptedFiletypes = new string[] { ".iso", ".cso", ".cci", ".zip", ".rar", ".7z" };
                    var tempFiles = Directory.GetFileSystemEntries(config.InputPath, "*", new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = config.RecurseInput, MatchCasing = MatchCasing.CaseInsensitive })
                        .Where(file => acceptedFiletypes.Contains(Path.GetExtension(file), StringComparer.CurrentCultureIgnoreCase))
                        .ToList();

                    var files = new List<string>();
                    for (int i = 0; i < tempFiles.Count; i++)
                    {
                        var nameWithoutExtension = Path.GetFileNameWithoutExtension(tempFiles[i]);
                        var subExtension = Path.GetExtension(nameWithoutExtension);
                        if (subExtension.Equals(".2"))
                        {
                            continue;
                        }
                        files.Add(tempFiles[i]);
                    }

                    for (int i = 0; i < files.Count; i++)
                    {
                        string? file = files[i];
                        CurrentProgress.Progress1 = i / (float)files.Count;
                        CurrentProgress.Progress1Text = $"Processing {i + 1} of {files.Count}";
                        SendProgress();

                        var gameIndex = ProcessFile(file, config.OutputPath, config.Grouping, crcMissingCount == 0, config.UpperCase, config.Compress, config.TrimmedScrub, cancellationToken);
                        if (gameIndex >= 0)
                        {
                            gameData[gameIndex].Process = "N";
                            GameDataHelper.SaveGameData(gameData);
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            Log(LogMessageLevel.None, "");
                            Log(LogMessageLevel.Info, "Cancelled.");
                            break;
                        }

                        Log(LogMessageLevel.None, "");
                    }
                }

                CurrentProgress.Progress1 = 1.0f;
                SendProgress();

                stopwatch.Stop();

                Log(LogMessageLevel.Done, $"Completed Processing List (Time Taken {stopwatch.Elapsed.TotalHours:00}:{stopwatch.Elapsed.Minutes:00}:{stopwatch.Elapsed.Seconds:00}).");
            }
            catch (Exception ex)
            {
                Log(LogMessageLevel.Error, $"Exception occured '{ex}'.");
            }
        }
    }
}

using Resurgent.UtilityBelt.Library.Utilities;
using Resurgent.UtilityBelt.Library.Utilities.XbeModels;
using SharpCompress.Archives;
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

        private int ProcessFile(string inputFile, string outputPath, GroupingEnum grouping, bool hasAllCrcs, bool upperCase, bool compress, CancellationToken cancellationToken)
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
                if (extension.Equals(".iso"))
                {
                    return ProcessIso(inputFile, outputPath, grouping, hasAllCrcs, upperCase, compress, cancellationToken);
                }

                return ProcessArchive(inputFile, outputPath, grouping, hasAllCrcs, upperCase, compress, cancellationToken);
            }
            catch (Exception ex)
            {
                Log(LogMessageLevel.Error, $"Processing '{inputFile}' caused error '{ex}'.");
                return -1;
            }
        }

        public int ProcessArchive(string inputFile, string outputPath, GroupingEnum grouping, bool hasAllCrcs, bool upperCase, bool compress, CancellationToken cancellationToken)
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
                        Directory.Delete(processOutput, true);
                    }
                    catch (IOException)
                    {
                        Log(LogMessageLevel.Error, $"Failed to delete directory '{processOutput}, close any windows accessing the folder.");
                        return -1;
                    }
                }
                Directory.CreateDirectory(unpackPath);

                var needsSecondPass = false;

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
                                Log(LogMessageLevel.Info, "Extracting ISO...");

                                var willScrub = tempGameData == null ? false : tempGameData.Value.Scrub.Equals("Y", StringComparison.CurrentCultureIgnoreCase);
                                if (tempGameData == null || compress == true || willScrub == true)
                                {
                                    using (var fileStream = new FileStream(Path.Combine(unpackPath, @"Repackinator.temp"), FileMode.Create))
                                    {
                                        var extractProgress = new Action<float>((progress) =>
                                        {
                                            CurrentProgress.Progress2 = progress;
                                            CurrentProgress.Progress2Text = $"Extracting ISO...";
                                            SendProgress();
                                        });
                                        using (var progressStream = new ProgressStream(fileStream, (long)entry.Size, extractProgress, cancellationToken))
                                        {
                                            entry.WriteTo(progressStream);
                                        }
                                    }
                                    needsSecondPass = true;
                                }
                                else
                                {
                                    Log(LogMessageLevel.Info, "Extracting And Splitting ISO...");

                                    using (var fileStream1 = new FileStream(Path.Combine(unpackPath, @"Repackinator.1.temp"), FileMode.Create))
                                    using (var fileStream2 = new FileStream(Path.Combine(unpackPath, @"Repackinator.2.temp"), FileMode.Create))
                                    {
                                        var extractProgress = new Action<float>((progress) =>
                                        {
                                            CurrentProgress.Progress2 = progress;
                                            CurrentProgress.Progress2Text = $"Extracting And Splitting ISO...";
                                            SendProgress();
                                        });
                                        using (var extractSplitStream = new ExtractSplitStream(fileStream1, fileStream2, (long)entry.Size, extractProgress, cancellationToken))
                                        {
                                            entry.WriteTo(extractSplitStream);
                                        }
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

                var xbeData = Array.Empty<byte>();
                if (needsSecondPass)
                {
                    using (var inputStream = new FileStream(Path.Combine(unpackPath, @"Repackinator.temp"), FileMode.Open))
                    using (var outputStream = new MemoryStream())
                    {
                        var error = string.Empty;
                        if (XisoUtility.TryExtractDefaultFromXiso(inputStream, outputStream, ref error))
                        {
                            xbeData = outputStream.ToArray();
                        }
                        else
                        {
                            Log(LogMessageLevel.Error, $"Unable to extract default.xbe due to '{error}'.");
                            return -1;
                        }
                    }
                }
                else
                {
                    using (var inputStream1 = new FileStream(Path.Combine(unpackPath, @"Repackinator.1.temp"), FileMode.Open))
                    using (var inputStream2 = new FileStream(Path.Combine(unpackPath, @"Repackinator.2.temp"), FileMode.Open))
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
                            return -1;
                        }
                    }
                }

                if (!XbeUtility.TryGetXbeCert(xbeData, out var cert) || cert == null)
                {
                    Log(LogMessageLevel.Error, $"Unable to get data from default.xbe.");
                    return -1;
                }

                var titleId = cert.Value.Title_Id.ToString("X8");
                var gameRegion = XbeCertificate.GameRegionToString(cert.Value.Game_Region);
                var version = cert.Value.Version.ToString("X8");

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

                        var repackProgress = new Action<float>((progress) =>
                        {
                            CurrentProgress.Progress2 = progress;
                            CurrentProgress.Progress2Text = message;
                            SendProgress();
                        });

                        if (!XisoUtility.CreateCCI(Path.Combine(unpackPath, @"Repackinator.temp"), processOutput, isoFileName, ".cci", scrub, repackProgress, cancellationToken))
                        {
                            Log(LogMessageLevel.Error, $"Unable process file 'Repackinator.temp'.");
                            return -1;
                        }
                    }
                    else
                    {
                        var message = $"Creating ISO...";

                        Log(LogMessageLevel.Info, message);

                        var repackProgress = new Action<float>((progress) =>
                        {
                            CurrentProgress.Progress2 = progress;
                            CurrentProgress.Progress2Text = message;
                            SendProgress();
                        });

                        if (!XisoUtility.Split(Path.Combine(unpackPath, @"Repackinator.temp"), processOutput, isoFileName, ".iso", scrub, repackProgress, cancellationToken))
                        {
                            Log(LogMessageLevel.Error, $"Unable process file 'Repackinator.temp'.");
                            return -1;
                        }
                    }

                    CurrentProgress.Progress2 = 1.0f;
                    SendProgress();

                    File.Delete(Path.Combine(unpackPath, @"Repackinator.temp"));
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

        public int ProcessIso(string inputFile, string outputPath, GroupingEnum grouping, bool hasAllCrcs, bool upperCase, bool compress, CancellationToken cancellationToken)
        {
            if (GameDataList == null)
            {
                Log(LogMessageLevel.Error, "GameData should not be null.");
                return -1;
            }

            var processOutput = string.Empty;
            var deleteProcessOutput = false;

            try
            {
                var processStopwatch = new Stopwatch();
                processStopwatch.Start();

                var xbeData = Array.Empty<byte>();
                using (var inputStream = new FileStream(inputFile, FileMode.Open))
                using (var outputStream = new MemoryStream())
                {
                    var error = string.Empty;
                    if (XisoUtility.TryExtractDefaultFromXiso(inputStream, outputStream, ref error))
                    {
                        xbeData = outputStream.ToArray();
                    }
                    else
                    {
                        Log(LogMessageLevel.Error, $"Unable to extract default.xbe due to '{error}'.");
                        return -1;
                    }
                }

                if (!XbeUtility.TryGetXbeCert(xbeData, out var cert) || cert == null)
                {
                    Log(LogMessageLevel.Error, $"Unable to get data from default.xbe.");
                    return -1;
                }

                var titleId = cert.Value.Title_Id.ToString("X8");
                var gameRegion = XbeCertificate.GameRegionToString(cert.Value.Game_Region);
                var version = cert.Value.Version.ToString("X8");

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
                    catch ( IOException )
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

                    var repackProgress = new Action<float>((progress) =>
                    {
                        CurrentProgress.Progress2 = progress;
                        CurrentProgress.Progress2Text = message;
                        SendProgress();
                    });

                    if (!XisoUtility.CreateCCI(inputFile, processOutput, isoFileName, ".cci", scrub, repackProgress, cancellationToken))
                    {
                        Log(LogMessageLevel.Error, $"Unable process file '{inputFile}'.");
                        return -1;
                    }
                }
                else
                {
                    var message = $"Creating ISO...";

                    Log(LogMessageLevel.Info, message);

                    var repackProgress = new Action<float>((progress) =>
                    {
                        CurrentProgress.Progress2 = progress;
                        CurrentProgress.Progress2Text = message;
                        SendProgress();
                    });

                    if (!XisoUtility.Split(inputFile, processOutput, isoFileName, ".iso", scrub, repackProgress, cancellationToken))
                    {
                        Log(LogMessageLevel.Error, $"Unable process file '{inputFile}'.");
                        return -1;
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

                var acceptedFiletypes = new List<string> { ".iso", ".zip", ".rar", ".7z"};
                var files = Directory.GetFileSystemEntries(config.InputPath, "*", SearchOption.AllDirectories)
                    .Where(file => acceptedFiletypes.Contains(Path.GetExtension(file).ToLower()))
                    .ToArray();

                for (int i = 0; i < files.Length; i++)
                {
                    string? file = files[i];
                    CurrentProgress.Progress1 = i / (float)files.Length;
                    CurrentProgress.Progress1Text = $"Processing {i + 1} of {files.Length}";
                    SendProgress();

                    var gameIndex = ProcessFile(file, config.OutputPath, config.Grouping, crcMissingCount == 0, config.UpperCase, config.Compress, cancellationToken);
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

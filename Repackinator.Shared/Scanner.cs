using Resurgent.UtilityBelt.Library.Utilities;
using Resurgent.UtilityBelt.Library.Utilities.XbeModels;
using SharpCompress;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Repackinator.Shared
{
    public class Scanner
    {
        private Action<LogMessage>? Logger { get; set; }

        private Action<ProgressInfo>? Progress { get; set; }

        private ProgressInfo CurrentProgress = new ProgressInfo();

        public GameData[]? GameDataList { get; set; }

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
            using var logStream = File.OpenWrite("ScanLog.txt");
            logStream.Write(bytes);
        }

        private void ProcessFolder(string folder, Stopwatch procesTime, CancellationToken cancellationToken)
        {
            if (GameDataList == null)
            {
                Log(LogMessageLevel.Error, "GameData should not be null.");
                return;
            }

            try
            {
                CurrentProgress.Progress2 = 0;
                CurrentProgress.Progress2Text = folder;
                SendProgress();

                var filesToProcess = Directory.GetFiles(folder, "*.iso").OrderBy(o => o).ToArray();
                if (filesToProcess.Length == 0)
                {
                    return;
                }

                if (filesToProcess.Length != 2)
                {
                    Log(LogMessageLevel.Error, $"Unexpected ISO count in folder '{folder}'.");
                    return;
                }

                var xbeData = Array.Empty<byte>();
                using (var inputStream1 = new FileStream(filesToProcess[0], FileMode.Open))
                using (var inputStream2 = new FileStream(filesToProcess[1], FileMode.Open))
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
                var xbeTitle = string.Empty;

                bool found = false;
                for (int i = 0; i < GameDataList.Length; i++)
                {
                    if (GameDataList[i].TitleID == titleId && GameDataList[i].Region == gameRegion && GameDataList[i].Version == version)
                    {
                        found = true;
                        GameDataList[i].Process = "N";
                        xbeTitle = GameDataList[i].XBETitle;
                        break;
                    }
                }

                if (found)
                {
                    Log(LogMessageLevel.Completed, $"Game found '{xbeTitle}'.");
                }
                else
                {
                    Log(LogMessageLevel.Warning, $"Game not found with TitleID = {titleId}, Region = '{gameRegion}', Version = {version}.");
                }

                CurrentProgress.Progress2 = 1.0f;
                SendProgress();
            }
            catch (Exception ex)
            {
                Log(LogMessageLevel.Error, $"Scanning '{folder}' caused error '{ex}'.");
            }
        }

        public bool StartScanning(GameData[]? gameData, Config config, Action<ProgressInfo>? progress, Action<LogMessage> logger, Stopwatch stopwatch, CancellationToken cancellationToken)
        {
            try
            {
                Logger = logger;
                Progress = progress;

                GameDataList = gameData;
                if (GameDataList == null)
                {
                    Log(LogMessageLevel.Error, "RepackList.json not found.");
                    return false;
                }

                stopwatch.Restart();

                if (File.Exists("ScanLog.txt"))
                {
                    File.Delete("ScanLog.txt");
                }

                for (var i = 0; i < GameDataList.Length; i++)
                {
                    GameDataList[i].Process = "Y";
                }

                CurrentProgress.Progress1 = 0;
                CurrentProgress.Progress1Text = "Searching Directories";
                CurrentProgress.Progress2 = 0;
                CurrentProgress.Progress2Text = string.Empty;
                SendProgress();

                var pathsScanned = 0;
                var totalPaths = 1;
                var pathsToScan = new List<string>();
                pathsToScan.Add(config.OutputPath);
                while (pathsToScan.Count > 0)
                {
                    var pathToProcess = pathsToScan[0];
                    pathsToScan.RemoveAt(0);

                    CurrentProgress.Progress1 = pathsScanned / (float)totalPaths;                   
                    SendProgress();

                    ProcessFolder(pathToProcess, stopwatch, cancellationToken);

                    try
                    {
                        var pathsToAdd = Directory.GetDirectories(pathToProcess);
                        pathsToScan.AddRange(pathsToAdd);
                        totalPaths += pathsToAdd.Length;
                    }
                    catch (Exception ex)
                    {
                        Log(LogMessageLevel.Error, $"Unable to get folders in '{pathToProcess}' as caused error '{ex}'.");
                    }

                    pathsScanned++;

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }

                CurrentProgress.Progress1 = 1.0f;
                SendProgress();

                stopwatch.Stop();

                Log(LogMessageLevel.Done, $"Completed Scanning List (Time Taken {stopwatch.Elapsed.Hours:00}:{stopwatch.Elapsed.Minutes:00}:{stopwatch.Elapsed.Seconds:00}).");
                return true;
            }
            catch (Exception ex)
            {
                Log(LogMessageLevel.Error, $"Exception occured '{ex}'.");
            }
            return false;
        }
    }
}

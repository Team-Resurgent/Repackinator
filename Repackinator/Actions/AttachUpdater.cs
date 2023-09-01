﻿using Repackinator.Helpers;
using Repackinator.Logging;
using Repackinator.Models;
using Resurgent.UtilityBelt.Library.Utilities;
using Resurgent.UtilityBelt.Library.Utilities.XbeModels;
using System.Diagnostics;

namespace Repackinator.Actions
{
    public class AttachUpdater
    {
        private Action<LogMessage>? Logger { get; set; }

        private Action<ProgressInfo>? Progress { get; set; }

        private ProgressInfo CurrentProgress = new ProgressInfo();

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
            File.AppendAllText("AttachUpdateLog.txt", logMessage.ToLogFormat());
        }

        private void ProcessFolder(string folder, Stopwatch procesTime, bool upperCase, GameData[]? gameDatas, CancellationToken cancellationToken)
        {
            var filesToProcess = Directory.GetFiles(folder, "default.xbe").OrderBy(o => o).ToArray();
            if (filesToProcess.Length != 1)
            {
                return;
            }

            try
            {
                CurrentProgress.Progress2 = 0;
                CurrentProgress.Progress2Text = folder;
                SendProgress();

                var xbeData = File.ReadAllBytes(filesToProcess[0]);

                if (!XbeUtility.TryGetXbeCert(xbeData, out var cert) || cert == null)
                {
                    Log(LogMessageLevel.Error, $"Unable to get data from default.xbe.");
                    return;
                }

                var titleId = cert.Title_Id.ToString("X8");
                var region = XbeCertificate.GameRegionToString(cert.Game_Region);
                var version = cert.Version.ToString("X8");

                var titleName = StringHelper.GetUnicodeString(cert.Title_Name);

                //GameData? gameData = null;
                if (gameDatas != null)
                {
                    foreach (var currentGameData in gameDatas)
                    {
                        if (currentGameData.TitleID != titleId || currentGameData.Region != region || currentGameData.Version != version)
                        {
                            continue;
                        }
                        //gameData = currentGameData;
                        titleName = currentGameData.XBETitle;
                    }
                }

                if (upperCase)
                {
                    titleName = titleName.ToUpper();
                }

                var attach = ResourceLoader.GetEmbeddedResourceBytes("attach.xbe");
                if (XbeUtility.TryGetXbeImage(xbeData, XbeUtility.ImageType.TitleImage, out var xprImage))
                {
                    if (XprUtility.ConvertXprToJpeg(xprImage, out var jpgImage))
                    {
                        if (!XbeUtility.TryReplaceXbeTitleImage(attach, jpgImage))
                        {
                            Log(LogMessageLevel.Error, "Failed to replace image.");
                            return;
                        }
                    }
                    else
                    {
                        Log(LogMessageLevel.Error, "Failed to create jpg.");
                        return;
                    }
                }
                else
                {
                    Log(LogMessageLevel.Warning, "Failed to extract xpr as probably missing, will use default image.");
                }

                if (XbeUtility.ReplaceCertInfo(attach, xbeData, titleName, out var patchedAttach) && patchedAttach != null)
                {
                    File.WriteAllBytes(filesToProcess[0], patchedAttach);
                }
                else
                {
                    Log(LogMessageLevel.Error, "failed creating attach xbe.");
                    return;
                }

                Log(LogMessageLevel.Completed, $"Updated '{filesToProcess[0]}'.");

                CurrentProgress.Progress2 = 1.0f;
                SendProgress();
            }
            catch (Exception ex)
            {
                Log(LogMessageLevel.Error, $"Attach Updating '{filesToProcess[0]}' caused error '{ex}'.");
            }
        }

        public bool StartAttachUpdating(GameData[]? gameData, Config config, Action<ProgressInfo>? progress, Action<LogMessage> logger, Stopwatch stopwatch, CancellationToken cancellationToken)
        {
            try
            {
                Logger = logger;
                Progress = progress;

                stopwatch.Restart();

                if (File.Exists("AttachUpdateLog.txt"))
                {
                    File.Delete("AttachUpdateLog.txt");
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

                    ProcessFolder(pathToProcess, stopwatch, config.UpperCase, gameData, cancellationToken);

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

                Log(LogMessageLevel.Done, $"Completed Attach Updating (Time Taken {stopwatch.Elapsed.TotalHours:00}:{stopwatch.Elapsed.Minutes:00}:{stopwatch.Elapsed.Seconds:00}).");
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

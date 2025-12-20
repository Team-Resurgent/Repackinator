using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using Repackinator.Core.Actions;
using Repackinator.Core.Logging;
using Repackinator.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Repackinator.ViewModels
{
    public class ScanOutputViewModel : ViewModelBase
    {
        private DispatcherTimer mTimer { get; set; }

        private Stopwatch _stopwatch = new();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public Window Owner { get; set; }

        public GameData[] GameDataList { get; set; }

        public Config Config { get; set; }

        public ObservableCollection<LogMessage> Log { get; set; }

        private string mProgress1Text = string.Empty;
        public string Progress1Text
        {
            get => mProgress1Text;
            set => this.RaiseAndSetIfChanged(ref mProgress1Text, value);
        }

        private float mProgress1 = 0;
        public float Progress1
        {
            get => mProgress1;
            set => this.RaiseAndSetIfChanged(ref mProgress1, value);
        }

        private string mProgress2Text = string.Empty;
        public string Progress2Text
        {
            get => mProgress2Text;
            set => this.RaiseAndSetIfChanged(ref mProgress2Text, value);
        }

        private float mProgress2 = 0;
        public float Progress2
        {
            get => mProgress2;
            set => this.RaiseAndSetIfChanged(ref mProgress2, value);
        }

        private string mSummary = "Totals: Warnings = 0, Errors = 0, Skipped = 0, Missing = 0, Completed = 0";
        public string Summary
        {
            get => mSummary;
            set => this.RaiseAndSetIfChanged(ref mSummary, value);
        }

        private string mTotalTime = "Total Time: 00:00:00";
        public string TotalTime
        {
            get => mTotalTime;
            set => this.RaiseAndSetIfChanged(ref mTotalTime, value);
        }

        public ICommand CloseCommand { get; set; }

        public ICommand CopyLogCommand { get; set; }

        private void RefreshDetails(object? sender, EventArgs e)
        {
            var totalWarnings = 0;
            var totalErrors = 0;
            var totalSkipped = 0;
            var totalNotFound = 0;
            var totalCompleted = 0;

            for (var i = 0; i < Log.Count; i++)
            {
                var logMessageLevel = Log[i].Level;
                if (logMessageLevel == LogMessageLevel.Warning)
                {
                    totalWarnings++;
                }
                else if (logMessageLevel == LogMessageLevel.Error)
                {
                    totalErrors++;
                }
                else if (logMessageLevel == LogMessageLevel.Skipped)
                {
                    totalSkipped++;
                }
                else if (logMessageLevel == LogMessageLevel.NotFound)
                {
                    totalNotFound++;
                }
                else if (logMessageLevel == LogMessageLevel.Completed)
                {
                    totalCompleted++;
                }
            }

            Summary = $"Totals: Warnings = {totalWarnings}, Errors = {totalErrors}, Skipped = {totalSkipped}, Missing = {totalNotFound}, Completed = {totalCompleted}";
            TotalTime = $"Total Time: {_stopwatch.Elapsed.Hours:00}:{_stopwatch.Elapsed.Minutes:00}:{_stopwatch.Elapsed.Seconds:00}";
        }

        public ScanOutputViewModel(Window owner, GameData[] gameDataList, Config config)
        {
            Log = [];
            Owner = owner;
            GameDataList = gameDataList;
            Config = config;

            CloseCommand = ReactiveCommand.Create(() =>
            {
                _cancellationTokenSource.Cancel();
                owner.Close();
            });

            CopyLogCommand = ReactiveCommand.Create(() =>
            {
                var logText = new StringBuilder();
                for (var i = 0; i < Log.Count; i++)
                {
                    logText.Append(Log[i].ToLogFormat());
                }
                owner.Clipboard?.SetTextAsync(logText.ToString());
            });

            mTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            mTimer.Tick += RefreshDetails;
            mTimer.Start();
        }

        public async Task<GameData[]?> StartAsync()
        {
            return await Task.Run(() =>
            {
                var logger = new Action<LogMessage>((logMessage) =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        Log.Add(logMessage);
                    });
                });

                var progress = new Action<ProgressInfo>((progress) =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        Progress1Text = progress.Progress1Text;
                        Progress1 = progress.Progress1;
                        Progress2Text = progress.Progress2Text;
                        Progress2 = progress.Progress2;
                    });
                });

                var scanner = new Scanner();
                var success = scanner.StartScanning(GameDataList, Config, progress, logger, _stopwatch, _cancellationTokenSource.Token);
                if (success == true)
                {
                    return scanner.GameDataList;
                }
                return null;
            });
        }

    }
   
}

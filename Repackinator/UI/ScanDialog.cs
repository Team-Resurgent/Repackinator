using ImGuiNET;
using Repackinator.Localization.Language;
using Repackinator.Logging;
using Repackinator.Models;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Repackinator.UI
{
    public class ScanDialog
    {
        private string _progress1Text = string.Empty;
        private float _progress1 = 0f;
        private string _progress2Text = string.Empty;
        private float _progress2 = 0f;
        private List<LogMessage> _log = new();
        private Config _config;
        private GameData[]? _gameData;
        private Stopwatch _stopwatch = new();
        private CancellationTokenSource _cancellationTokenSource = new();

        private bool _showModal;
        private bool _open;
        private bool _completed;
        private bool _logChanged;

        public GameData[]? GameDataList => _gameData;

        public void ShowModal(Config config, GameData[]? gameData)
        {
            _config = config;

            if (gameData != null)
            {
                _gameData = new GameData[gameData.Length];
                Array.Copy(gameData, _gameData, gameData.Length);
            }
            else
            {
                _gameData = null;
            }

            _showModal = true;
        }

        private void CloseModal()
        {
            _open = false;
            ImGui.CloseCurrentPopup();
        }

        private void Scan()
        {
            var logger = new Action<LogMessage>((logMessage) =>
            {
                _log.Add(logMessage);
                _logChanged = true;
            });

            var progress = new Action<ProgressInfo>((progress) =>
            {
                _progress1 = progress.Progress1;
                _progress1Text = progress.Progress1Text;
                _progress2 = progress.Progress2;
                _progress2Text = progress.Progress2Text;
            });

            _cancellationTokenSource = new CancellationTokenSource();

            var scanner = new Actions.Scanner();
            var success = scanner.StartScanning(_gameData, _config, progress, logger, _stopwatch, _cancellationTokenSource.Token);
            if (success == true)
            {
                _gameData = scanner.GameDataList;
            }

            _completed = true;
        }

        public bool Render()
        {
            if (_showModal)
            {
                _showModal = false;

                _completed = false;
                _progress1Text = string.Empty;
                _progress1 = 0;
                _progress2Text = string.Empty;
                _progress2 = 0;

                _log = new();

                var scanThread = new Thread(Scan);
                scanThread.Start();

                _open = true;
                ImGui.OpenPopup(UserLocale.scandialog_title);
            }

            if (!_open)
            {
                return false;
            }

            var open = true;
            if (!ImGui.BeginPopupModal(UserLocale.scandialog_title, ref open))
            {
                _cancellationTokenSource.Cancel();
                return false;
            }

            var result = false;

            if (ImGui.IsWindowAppearing())
            {
                ImGui.SetWindowSize(new Vector2(700, 400));
            }

            Vector2 windowSize = ImGui.GetWindowSize();

            ImGui.Text(_progress1Text);
            ImGui.ProgressBar(_progress1, new Vector2(windowSize.X - 16, 20));
            ImGui.Spacing();
            ImGui.Text(_progress2Text);
            ImGui.ProgressBar(_progress2, new Vector2(windowSize.X - 16, 20));
            ImGui.Spacing();

            var totalWarnings = 0;
            var totalErrors = 0;
            var totalSkipped = 0;
            var totalNotFound = 0;
            var totalCompleted = 0;

            ImGuiTableFlags flags = ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.RowBg;
            if (ImGui.BeginTable("table_sorting", 3, flags, new Vector2(windowSize.X - 16, windowSize.Y - 185), 0.0f))
            {
                ImGui.TableSetupColumn(UserLocale.scandialog_log_table_time, ImGuiTableColumnFlags.WidthFixed, 75.0f, 0);
                ImGui.TableSetupColumn(UserLocale.scandialog_log_table_level, ImGuiTableColumnFlags.WidthFixed, 75.0f, 1);
                ImGui.TableSetupColumn(UserLocale.scandialog_log_table_message, ImGuiTableColumnFlags.WidthStretch, 300.0f, 2);
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                for (var i = 0; i < _log.Count; i++)
                {
                    var logEntry = _log[i];
                    var logColor = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
                    if (logEntry.Level == LogMessageLevel.Warning)
                    {
                        totalWarnings++;
                        logColor = new Vector4(1, 0.75f, 0f, 1);
                    }
                    else if (logEntry.Level == LogMessageLevel.Error)
                    {
                        totalErrors++;
                        logColor = new Vector4(1, 0.25f, 0.25f, 1);
                    }
                    else if (logEntry.Level == LogMessageLevel.Skipped)
                    {
                        totalSkipped++;
                        logColor = new Vector4(1, 1, 0f, 1);
                    }
                    else if (logEntry.Level == LogMessageLevel.NotFound)
                    {
                        totalNotFound++;
                        logColor = new Vector4(0.25f, 0.25f, 1f, 1);
                    }
                    else if (logEntry.Level == LogMessageLevel.Completed)
                    {
                        totalCompleted++;
                        logColor = new Vector4(0.25f, 1, 0.25f, 1);
                    }
                    else if (logEntry.Level == LogMessageLevel.Done)
                    {
                        logColor = new Vector4(0.25f, 1, 0.25f, 1);
                    }

                    ImGui.PushID(i);
                    ImGui.TableNextRow(ImGuiTableRowFlags.None, 22);

                    ImGui.TableNextColumn();
                    ImGui.Text(logEntry.Level == LogMessageLevel.None ? string.Empty : logEntry.Time.ToString("HH:mm:ss"));

                    ImGui.PushStyleColor(ImGuiCol.Text, ImGui.ColorConvertFloat4ToU32(logColor));
                    ImGui.TableNextColumn();
                    ImGui.Text(logEntry.Level == LogMessageLevel.None ? string.Empty : logEntry.LogLevel);
                    ImGui.PopStyleColor();

                    ImGui.TableNextColumn();
                    ImGui.Text(logEntry.Level == LogMessageLevel.None ? string.Empty : logEntry.Message);

                    ImGui.PopID();
                }

                if (_logChanged)
                {
                    _logChanged = false;
                    ImGui.SetScrollHereY();
                }
                ImGui.EndTable();
            }

            ImGui.Text(string.Format(UserLocale.scandialog_totals, totalWarnings, totalErrors, totalSkipped, totalNotFound, totalCompleted));

            ImGui.SameLine();

            var timeTaken = string.Format(UserLocale.scandialog_total_time_elapsed, _stopwatch.Elapsed.TotalHours, _stopwatch.Elapsed.Minutes, _stopwatch.Elapsed.Seconds);
            ImGui.SetCursorPosX(windowSize.X - ImGui.CalcTextSize(timeTaken).X - 8);
            ImGui.Text(timeTaken);

            ImGui.SetCursorPosY(windowSize.Y - 40);

            if (ImGui.Button(_completed ? UserLocale.scandialog_button_close : _cancellationTokenSource.IsCancellationRequested ? UserLocale.scandialog_button_cancelling : UserLocale.scandialog_button_cancel, new Vector2(100, 30)))
            {
                if (!_completed)
                {
                    _cancellationTokenSource.Cancel();
                }
                else
                {
                    result = true;
                    CloseModal();
                }
            }

            ImGui.SameLine();

            if (ImGui.Button(UserLocale.scandialog_button_copy_log, new Vector2(100, 30)))
            {
                var logText = new StringBuilder();
                for (var i = 0; i < _log.Count; i++)
                {
                    logText.Append(_log[i].ToLogFormat());
                }
                ImGui.SetClipboardText(logText.ToString());
            }

            ImGui.EndPopup();

            return result;
        }
    }
}
using System.Numerics;
using System.Text;
using ImGuiNET;
using Repackinator.Shared;

namespace RepackinatorUI
{
    public class RepackDialog
    {
        private string _progress1Text = string.Empty;
        private float _progress1 = 0f;
        private string _progress2Text = string.Empty;
        private float _progress2 = 0f;
        private string _log = string.Empty;
        private Config _config;
        private GameData[]? _gameData;
        private CancellationTokenSource _cancellationTokenSource = new();

        private bool _showModal;
        private bool _open;
        private bool _completed;

        public void ShowModal(Config config, GameData[]? gameData)
        {
            _config = config;
            _gameData = gameData;
            _showModal = true;
        }

        private void CloseModal()
        {
            _open = false;
            ImGui.CloseCurrentPopup();
        }

        void Repack()
        {
            var logger = new Action<LogMessage>((logMessage) =>
            {
                var formattedTime = logMessage.Time.ToString("HH:mm:ss");
                var message = $"{formattedTime} {logMessage.Level} - {logMessage.Message}";
                _log += $"{message}\n";
            });

            var progress = new Action<ProgressInfo>((progress) =>
            {
                _progress1 = progress.Progress1;
                _progress1Text = progress.Progress1Text;
                _progress2 = progress.Progress2;
                _progress2Text = progress.Progress2Text;
            });

            _cancellationTokenSource = new CancellationTokenSource();

            var repacker = new Repacker();
            repacker.StartConversion(_gameData, _config, progress, logger, _cancellationTokenSource.Token);

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

                _log = string.Empty; 

                var repackThread = new Thread(Repack);
                repackThread.Start();

                _open = true;
                ImGui.OpenPopup("Repacking");
            }

            if (!_open)
            {
                return false;
            }

            var open = true;
            if (!ImGui.BeginPopupModal("Repacking", ref open))
            {
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
            ImGui.InputTextMultiline("##reoackLog", ref _log, (uint)_log.Length, new Vector2(windowSize.X - 16, windowSize.Y - 175), ImGuiInputTextFlags.ReadOnly);
            
            ImGui.SetCursorPosY(windowSize.Y - 40);

            if (ImGui.Button(_completed ? "Close" : (_cancellationTokenSource.IsCancellationRequested ? "Cancelling..." : "Cancel"), new Vector2(100, 30)))
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
            ImGui.EndPopup();

            return result;
        }
    }
}
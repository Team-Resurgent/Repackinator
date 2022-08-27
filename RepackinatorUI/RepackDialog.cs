using System.Numerics;
using ImGuiNET;
using Repackinator.Shared;
using SharpDX.DXGI;

namespace RepackinatorUI
{
    public class RepackDialog
    {
        private string _progress1Text = string.Empty;
        private float _progress1 = 0f;
        private string _progress2Text = string.Empty;
        private float _progress2 = 0f;
        private string _log = string.Empty;
        private Config? _config;

        private bool _showModal;
        private bool _open;
        private bool _completed;

        public void ShowModal(Config config)
        {
            _config = config;
            _showModal = true;
        }

        private void CloseModal()
        {
            _open = false;
            ImGui.CloseCurrentPopup();
        }

        void Repack()
        {
            if (_config == null)
            {
                _log = "Error: Unable to repack due to null config.";
                _completed = true;
                return;
            }

            var repacker = new Repacker();
           // repacker.StartConversion(_config.InputPath, _config.OutputPath, _config.TempPath, groupingValue, _config.Alternative, null, logger);

            _log += "Dropping the F Bomb\n";
            for (int i = 0; i <= 1000; i++)
            {
                _progress1Text = $"Processing {i} of 1000";
                _progress1 = i / 1000.0f;
                Thread.Sleep(1);
            }

            _log += "Calculating Meaning of Life\n";
            for (int i = 0; i <= 1000; i++)
            {
                _progress2Text = $"Spliiting DVD {i / 10}%%";
                _progress2 = i / 1000.0f;
                Thread.Sleep(1);
            }

            _log += "Done\n";
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
            if (!ImGui.BeginPopupModal("Repacking", ref open, ImGuiWindowFlags.NoResize))
            {
                return false;
            }

            var result = false;

            if (ImGui.IsWindowAppearing())
            {
                ImGui.SetWindowSize(new Vector2(500, 300));
            }

            ImGui.Text(_progress1Text);
            ImGui.ProgressBar(_progress1, new Vector2(484, 20));
            ImGui.Spacing();
            ImGui.Text(_progress2Text);
            ImGui.ProgressBar(_progress2, new Vector2(484, 20));
            ImGui.Spacing();
            ImGui.InputTextMultiline("##reoackLog", ref _log, (uint)_log.Length, new Vector2(484, 125), ImGuiInputTextFlags.ReadOnly);
            
            ImGui.SetCursorPosY(300 - 40);

            if (ImGui.Button(_completed ? "Close" : "Cancel", new Vector2(100, 30)))
            {
                result = true;
                CloseModal();
            }
            ImGui.EndPopup();

            return result;
        }
    }
}
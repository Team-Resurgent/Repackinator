using System.Numerics;
using ImGuiNET;

namespace RepackinatorUI
{
    public class RepackDialog
    {
        private bool _showModal;
        private bool _open;

        public void ShowModal()
        {
            _showModal = true;
        }

        private void CloseModal()
        {
            _open = false;
            ImGui.CloseCurrentPopup();
        }

        public bool Render()
        {
            if (_showModal)
            {
                _showModal = false;
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

            string repackLog = "Log";
            ImGui.Text("Processing 1 of 1000");
            ImGui.ProgressBar(0.5f, new Vector2(484, 20));
            ImGui.Spacing();
            ImGui.Text("Spliiting DVD");
            ImGui.ProgressBar(0.25f, new Vector2(484, 20));
            ImGui.Spacing();
            ImGui.InputTextMultiline("##reoackLog", ref repackLog, (uint)repackLog.Length, new Vector2(484, 125), ImGuiInputTextFlags.ReadOnly);
            
            ImGui.SetCursorPosY(300 - 40);

            if (ImGui.Button("Cancel", new Vector2(100, 30)))
            {
                result = true;
                CloseModal();
            }
            ImGui.EndPopup();

            return result;
        }
    }
}
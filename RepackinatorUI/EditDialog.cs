using System.Numerics;
using ImGuiNET;

namespace RepackinatorUI
{
    public class EditDialog
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
                ImGui.OpenPopup("Edit Game");
            }

            if (!_open)
            {
                return false;
            }

            var open = true;
            if (!ImGui.BeginPopupModal("Edit Game", ref open, ImGuiWindowFlags.NoResize))
            {
                return false;
            }

            var result = false;

            if (ImGui.IsWindowAppearing())
            {
                ImGui.SetWindowSize(new Vector2(600, 400));
            }

            ImGui.Text("Editor coming soon");
            ImGui.Spacing();
            if (ImGui.Button("Ok", new Vector2(100, 30)))
            {
                result = true;
                CloseModal();
            }            
            ImGui.EndPopup();

            return result;
        }
    }
}
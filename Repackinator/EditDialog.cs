using ImGuiNET;
using Repackinator.Shared;
using System.Diagnostics;
using System.Numerics;

namespace Repackinator
{
    public class EditDialog
    {
        private bool _showModal;
        private bool _open;

        public int Index;
        public GameData GameData;

        public void ShowModal(GameData gameData, int index)
        {
            _showModal = true;
            Index = index;
            GameData = gameData;
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
            if (!ImGui.BeginPopupModal("Edit Game", ref open, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar))
            {
                return false;
            }

            var result = false;

            if (ImGui.IsWindowAppearing())
            {
                ImGui.SetWindowSize(new Vector2(410, 322));
            }

            var textColor = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];

            ImGui.TextUnformatted("TitleId:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(100);
            ImGui.TextUnformatted(GameData.TitleID);

            ImGui.TextUnformatted("Version:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(100);
            ImGui.TextUnformatted(GameData.Version);

            ImGui.TextUnformatted("Region:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(100);
            ImGui.TextUnformatted(GameData.Region);

            ImGui.TextUnformatted("TitleName:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(100);
            ImGui.TextUnformatted(GameData.TitleName);

            ImGui.TextUnformatted("Letter:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(100);
            string letter = GameData.Letter ?? "";
            ImGui.PushItemWidth(75.0f);
            if (ImGui.InputText("##editLetter", ref letter, 1))
            {
                GameData.Letter = letter;
            }
            ImGui.PopItemWidth();

            ImGui.TextUnformatted("XBE Title:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(100);
            string xbeTitle = GameData.XBETitle ?? "";
            ImGui.PushItemWidth(300);
            ImGui.PushStyleColor(ImGuiCol.Text, xbeTitle.Length > 40 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.25f, 0.25f, 1)) : ImGui.ColorConvertFloat4ToU32(textColor));
            if (ImGui.InputText("##editXbeTitle", ref xbeTitle, 40))
            {
                GameData.XBETitle = xbeTitle;
            }
            ImGui.PopStyleColor();
            ImGui.PopItemWidth();

            ImGui.TextUnformatted("Folder Name:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(100);
            string folderName = GameData.FolderName ?? "";
            ImGui.PushItemWidth(300);
            ImGui.PushStyleColor(ImGuiCol.Text, folderName.Length > 42 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.25f, 0.25f, 1)) : ImGui.ColorConvertFloat4ToU32(textColor));
            if (ImGui.InputText("##editFolderName", ref folderName, 42))
            {
                GameData.FolderName = folderName;
            }
            ImGui.PopStyleColor();
            ImGui.PopItemWidth();

            ImGui.TextUnformatted("ISO Name:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(100);
            string isoName = GameData.ISOName ?? "";
            ImGui.PushItemWidth(300);
            ImGui.PushStyleColor(ImGuiCol.Text, isoName.Length > 36 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.25f, 0.25f, 1)) : ImGui.ColorConvertFloat4ToU32(textColor));
            if (ImGui.InputText("##editIsoName", ref isoName, 36))
            {
                GameData.ISOName = isoName;
            }
            ImGui.PopStyleColor();
            ImGui.PopItemWidth();

            ImGui.TextUnformatted("ISO Checksum:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(100);
            string isoChecksum = GameData.ISOChecksum ?? "";
            ImGui.PushItemWidth(300);
            if (ImGui.InputText("##editIsoChecksum", ref isoChecksum, 8))
            {
                GameData.ISOChecksum = isoChecksum.ToUpper();
            }
            ImGui.PopItemWidth();

            string link = GameData.Link ?? "";

            ImGui.TextUnformatted("Link:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(8);
            if (ImGui.InvisibleButton("##link", new Vector2(80, 20)))
            {
                if (OperatingSystem.IsWindows())
                {
                    try
                    {
                        Process.Start("cmd", "/C start" + " " + link);
                    }
                    catch
                    {
                        // do nothing
                    }
                }
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(100);
            ImGui.PushItemWidth(300);
            if (ImGui.InputText("##editLink", ref link, 8))
            {
                GameData.Link = link;
            }

            ImGui.PopItemWidth();

            string info = GameData.Info ?? "";

            ImGui.TextUnformatted("Info:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(8);
            if (ImGui.InvisibleButton("##info", new Vector2(80, 20)))
            {
                if (OperatingSystem.IsWindows())
                {
                    try
                    {
                        Process.Start("cmd", "/C start" + " " + info);
                    }
                    catch
                    {
                        // do nothing
                    }
                }
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(100);
            ImGui.PushItemWidth(300);
            if (ImGui.InputText("##editInfo", ref info, 8))
            {
                GameData.Info = info;
            }
            ImGui.PopItemWidth();

            ImGui.Spacing();
            if (ImGui.Button("Ok", new Vector2(100, 30)))
            {
                result = true;
                CloseModal();
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(100, 30)))
            {
                result = false;
                CloseModal();
            }
            ImGui.EndPopup();

            return result;
        }
    }
}
﻿using System.Numerics;
using ImGuiNET;
using Repackinator.Shared;

namespace RepackinatorUI
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
                ImGui.SetWindowSize(new Vector2(410, 348));
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
            ImGui.PushStyleColor(ImGuiCol.Text, xbeTitle.Length > 40 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.5f, 0.5f, 1)) : ImGui.ColorConvertFloat4ToU32(textColor));
            if (ImGui.InputText("##editXbeTitle", ref xbeTitle, 40))
            {
                GameData.XBETitle = xbeTitle;
            }
            ImGui.PopStyleColor();
            ImGui.PopItemWidth();

            ImGui.TextUnformatted("XBE Title Alt:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(100);
            string xbeTitleAlt = GameData.XBETitleAlt ?? "";
            ImGui.PushItemWidth(300);
            ImGui.PushStyleColor(ImGuiCol.Text, xbeTitleAlt.Length > 40 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.5f, 0.5f, 1)) : ImGui.ColorConvertFloat4ToU32(textColor));
            if (ImGui.InputText("##editXbeTitleAlt", ref xbeTitleAlt, 40))
            {
                GameData.XBETitleAlt = xbeTitleAlt;
            }
            ImGui.PopStyleColor();
            ImGui.PopItemWidth();

            ImGui.TextUnformatted("Folder Name:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(100);
            string folderName = GameData.FolderName ?? "";
            ImGui.PushItemWidth(300);
            ImGui.PushStyleColor(ImGuiCol.Text, folderName.Length > 42 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.5f, 0.5f, 1)) : ImGui.ColorConvertFloat4ToU32(textColor));
            if (ImGui.InputText("##editFolderName", ref folderName, 42))
            {
                GameData.FolderName = folderName;
            }
            ImGui.PopStyleColor();
            ImGui.PopItemWidth();

            ImGui.TextUnformatted("Folder Name Alt:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(100);
            string folderNameAlt = GameData.FolderNameAlt ?? "";
            ImGui.PushItemWidth(300);
            ImGui.PushStyleColor(ImGuiCol.Text, folderNameAlt.Length > 42 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.5f, 0.5f, 1)) : ImGui.ColorConvertFloat4ToU32(textColor));
            if (ImGui.InputText("##editFolderNameAlt", ref folderNameAlt, 42))
            {
                GameData.FolderNameAlt = folderNameAlt;
            }
            ImGui.PopStyleColor();
            ImGui.PopItemWidth();

            ImGui.TextUnformatted("ISO Name:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(100);
            string isoName = GameData.ISOName ?? "";
            ImGui.PushItemWidth(300);
            ImGui.PushStyleColor(ImGuiCol.Text, isoName.Length > 36 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.5f, 0.5f, 1)) : ImGui.ColorConvertFloat4ToU32(textColor));
            if (ImGui.InputText("##editIsoName", ref isoName, 36))
            {
                GameData.ISOName = isoName;
            }
            ImGui.PopStyleColor();
            ImGui.PopItemWidth();

            ImGui.TextUnformatted("ISO Name Alt:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(100);
            string isoNameAlt = GameData.ISONameAlt ?? "";
            ImGui.PushItemWidth(300);
            ImGui.PushStyleColor(ImGuiCol.Text, isoNameAlt.Length > 36 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.5f, 0.5f, 1)) : ImGui.ColorConvertFloat4ToU32(textColor));
            if (ImGui.InputText("##editIsoNameAlt{i}", ref isoNameAlt, 36))
            {
                GameData.ISONameAlt = isoNameAlt;
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
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;
using System.Runtime.InteropServices;

namespace RepackinatorUI
{
    public class PathPicker
    {
        public enum PickerMode
        {
            File,
            Folder
        }

        private static bool Like(string str, string pattern)
        {
            return new Regex("^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$", RegexOptions.IgnoreCase | RegexOptions.Singleline).IsMatch(str);
        }

        private bool _showModal;
        private bool _open;

        public PickerMode Mode { get; set; }

        private string _selectedFolder;
        public string SelectedFolder
        {
            get => _selectedFolder;
            set => _selectedFolder = value;
        }

        public string SelectedFile { get; private set; }
        public bool Cancelled { get; private set; }
        public string[] AllowedFiles { get; set; }
        public bool ShowHidden { get; set; }

        public void ShowModal(string path)
        {
            _showModal = true;
            SelectedFolder = path;
            SelectedFile = string.Empty;
        }

        private void CloseModal()
        {
            _open = false;
            ImGui.CloseCurrentPopup();
        }

        public PathPicker()
        {
            Mode = PickerMode.File;
            AllowedFiles = new[] { "*.*" };
            ShowHidden = false;
        }

        private static IEnumerable<string> GetSpecialFolders()
        {
            var specialFolders = new List<string>();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                specialFolders.Add($"/|/");
                specialFolders.Add($"User|{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}");
                specialFolders.Add($"Desktop|{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}");
                specialFolders.Add($"Documents|{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/Documents");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                specialFolders.Add($"User|{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}");
                specialFolders.Add($"Desktop|{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}");
                specialFolders.Add($"Documents|{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}");
            }

            var logicalDrives = Directory.GetLogicalDrives();
            foreach (var logicalDrive in logicalDrives)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    if (logicalDrive.StartsWith("/Volume", StringComparison.CurrentCultureIgnoreCase))
                    {
                        specialFolders.Add($"{Path.GetFileName(logicalDrive)}|{logicalDrive}");
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    specialFolders.Add($"{logicalDrive.Substring(0, 2)}|{logicalDrive}");
                }
            }

            return specialFolders.ToArray();
        }

        private static void DrawLines(IReadOnlyList<Vector2> points, Vector2 location, float size)
        {
            var iconColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1));
            var drawList = ImGui.GetWindowDrawList();
            for (var i = 0; i < points.Count; i += 2)
            {
                var vector1 = (points[i] / 100) * size;
                var vector2 = (points[i + 1] / 100) * size;
                drawList.AddLine(location + vector1, location + vector2, iconColor);
            }
        }

        private static void GenerateFileIcon(Vector2 location, float size)
        {
            var points = new[] {
                new Vector2(0.0f,0.0f), new Vector2(45.0f, 0.0f),
                new Vector2(45.0f,0.0f), new Vector2(55.0f, 22.5f),
                new Vector2(55.0f,22.5f), new Vector2(100.0f, 22.5f),
                new Vector2(100.0f,22.5f), new Vector2(100.0f, 87.5f),
                new Vector2(100.0f,87.5f), new Vector2(0.0f, 87.5f),
                new Vector2(0.0f,87.5f), new Vector2(0.0f, 0.0f)
            };
            DrawLines(points, location, size);
        }

        private static void GenerateFolderIcon(Vector2 location, float size)
        {
            var points = new[] {
                new Vector2(12.5f,0.0f), new Vector2(62.5f, 0.0f),
                new Vector2(62.5f,0.0f), new Vector2(87.5f, 50.0f),
                new Vector2(87.5f,50.0f), new Vector2(87.5f, 100.0f),
                new Vector2(87.5f,100.0f), new Vector2(12.5f, 100.0f),
                new Vector2(12.5f,100.0f), new Vector2(12.5f, 0.0f),
                new Vector2(62.5f,0.0f), new Vector2(62.5f, 50.0f),
                new Vector2(62.5f,50.0f), new Vector2(87.5f, 50.0f)
            };
            DrawLines(points, location, size);
        }

        private bool ProcessChildFolders(string path)
        {
            foreach (var fse in Directory.EnumerateFileSystemEntries(path))
            {
                var name = Path.GetFileName(fse);

                var attributes = File.GetAttributes(fse);
                var isHidden = (attributes & FileAttributes.Hidden) == FileAttributes.Hidden;

                if (!ShowHidden && isHidden)
                {
                    continue;
                }

                var isDirectory = (attributes & FileAttributes.Directory) == FileAttributes.Directory;
                if (!isDirectory)
                {
                    continue;
                }

                var iconPosition = ImGui.GetWindowPos() + ImGui.GetCursorPos();
                iconPosition.Y -= ImGui.GetScrollY();

                var lineHeight = ImGui.GetTextLineHeight();
                ImGui.SetCursorPosX(lineHeight * 2);

                if (ImGui.Selectable(name, false, ImGuiSelectableFlags.DontClosePopups))
                {
                    SelectedFile = string.Empty;
                    _selectedFolder = fse;
                }

                GenerateFileIcon(iconPosition, lineHeight);
            }

            return false;
        }

        private bool ProcessChildFiles(string path)
        {
            if (Mode == PickerMode.Folder)
            {
                return false;
            }

            var result = false;

            foreach (var fse in Directory.EnumerateFileSystemEntries(path))
            {
                var name = Path.GetFileName(fse);

                var attributes = File.GetAttributes(fse);
                var isHidden = (attributes & FileAttributes.Hidden) == FileAttributes.Hidden;

                if (!ShowHidden && isHidden)
                {
                    continue;
                }

                var isDirectory = (attributes & FileAttributes.Directory) == FileAttributes.Directory;
                if (isDirectory)
                {
                    continue;
                }

                var allowed = AllowedFiles.Aggregate(false, (current, allowedFile) => current | Like(name, allowedFile));
                if (!allowed)
                {
                    continue;
                }

                var iconPosition = ImGui.GetWindowPos() + ImGui.GetCursorPos();
                iconPosition.Y -= ImGui.GetScrollY();

                var lineHeight = ImGui.GetTextLineHeight();
                ImGui.SetCursorPosX(lineHeight * 2);

                var isSelected = SelectedFile == fse;
                if (ImGui.Selectable(name, isSelected, ImGuiSelectableFlags.DontClosePopups | ImGuiSelectableFlags.AllowDoubleClick))
                {
                    SelectedFile = fse;
                    if (ImGui.IsMouseDoubleClicked(0))
                    {
                        Cancelled = false;
                        result = true;
                        ImGui.CloseCurrentPopup();
                    }
                }

                GenerateFolderIcon(iconPosition, lineHeight);
            }

            return result;
        }

        public bool Render()
        {
            if (_showModal)
            {
                _showModal = false;
                _open = true;
                ImGui.OpenPopup($"{Mode} Browser");
            }

            if (!_open)
            {
                return false;
            }

            if (!ImGui.BeginPopupModal($"{Mode} Browser"))
            {
                return false;
            }

            var result = false;

            if (ImGui.IsWindowAppearing())
            {
                ImGui.SetWindowSize(new Vector2(800, 600));
            }

            var size = ImGui.GetWindowSize();

            ImGui.PushItemWidth(size.X - 16);
            ImGui.InputText("###file-path", ref _selectedFolder, 300, ImGuiInputTextFlags.ReadOnly);
            ImGui.Spacing();

            if (ImGui.BeginChildFrame(1, new Vector2(200, size.Y - 100), ImGuiWindowFlags.None))
            {
                var specialFolders = GetSpecialFolders();
                foreach (var specialFolder in specialFolders)
                {
                    var parts = specialFolder.Split('|');
                    if (ImGui.Selectable(parts[0], false, ImGuiSelectableFlags.DontClosePopups))
                    {
                        _selectedFolder = parts[1];
                    }
                }
                ImGui.EndChildFrame();
            }

            ImGui.SameLine();
            if (ImGui.BeginChildFrame(2, new Vector2(size.X - 224, size.Y - 100), ImGuiWindowFlags.None))
            {
                var directoryInfo = new DirectoryInfo(_selectedFolder);
                if (directoryInfo.Parent != null)
                {
                    if (ImGui.Selectable("..", false, ImGuiSelectableFlags.DontClosePopups))
                    {
                        _selectedFolder = directoryInfo.Parent.FullName;
                    }
                }
                try
                {
                    result |= ProcessChildFolders(directoryInfo.FullName);
                    result |= ProcessChildFiles(directoryInfo.FullName);
                }
                catch
                {
                    Debug.Print($"Unable to process path '{directoryInfo.FullName}'.");
                }
                ImGui.EndChildFrame();
            }

            ImGui.Spacing();
            ImGui.SetCursorPosX(size.X - 216);
            if (ImGui.Button("Cancel", new Vector2(100, 30)))
            {
                Cancelled = true;
                result = true;
                CloseModal();
            }
            ImGui.SameLine();
            if (ImGui.Button("Open", new Vector2(100, 30)))
            {
                var valid = false;
                valid |= Mode == PickerMode.File && !string.IsNullOrEmpty(SelectedFile);
                valid |= Mode == PickerMode.Folder && !string.IsNullOrEmpty(SelectedFolder);
                if (valid)
                {
                    Cancelled = false;
                    result = true;
                    CloseModal();
                }
            }

            ImGui.EndPopup();

            return result;
        }
    }
}
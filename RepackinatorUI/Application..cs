using ImGuiNET;
using Repackinator.Shared;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

//TODO: on repack close cancel

namespace RepackinatorUI
{
    public class Application
    {
        private Sdl2Window? m_window;
        private GraphicsDevice? m_graphicsDevice;
        private CommandList? m_commandList;
        private ImGuiController? m_controller;
        private GameData[]? m_gameDataList;
        private PathPicker? m_inputFolderPicker;
        private PathPicker? m_outputFolderPicker;
        private PathPicker? m_exportFolderPicker;
        private EditDialog? m_editDialog;
        private OkDialog? m_okDialog;
        private CreditsDialog? m_creditsDialog;
        private RepackDialog? m_repackDialog;
        private ScanDialog? m_scanDialog;
        private Config m_config = new Config();

        private int m_searchField;
        private string? m_searchText;
        private int m_processField;
        private bool m_showInvalid;
        private string m_version;

        public Application(string version)
        {
            m_version = version;
        }

        private bool IsFiltered(int index)
        {
            if (string.IsNullOrEmpty(m_searchText) || m_gameDataList == null)
            {
                return false;
            }

            GameData gameData = m_gameDataList[index];

            if (m_searchField == 0 && gameData.Process != null)
            {
                return !gameData.Process.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 1 && gameData.TitleID != null)
            {
                return !gameData.TitleID.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 2 && gameData.Region != null)
            {
                return !gameData.Region.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 3 && gameData.Version != null)
            {
                return !gameData.Version.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 4 && gameData.TitleName != null)
            {
                return !gameData.TitleName.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 5 && gameData.Letter != null)
            {
                return !gameData.Letter.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 6 && gameData.XBETitle != null)
            {
                return !gameData.XBETitle.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 7 && gameData.XBETitleAlt != null)
            {
                return !gameData.XBETitleAlt.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 8 && gameData.FolderName != null)
            {
                return !gameData.FolderName.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 9 && gameData.FolderNameAlt != null)
            {
                return !gameData.FolderNameAlt.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 10 && gameData.ISOName != null)
            {
                return !gameData.ISOName.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 11 && gameData.ISONameAlt != null)
            {
                return !gameData.ISONameAlt.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 12 && gameData.ISOChecksum != null)
            {
                return !gameData.ISOChecksum.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            return false;
        }

        private bool ValidateFatX(string path)
        {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz 0123456789!#$%&'()-.@[]^_`{}~";
            foreach (var c in path)
            {
                if (!validChars.Contains(c))
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsValidRow(int index)
        {
            if (string.IsNullOrEmpty(m_searchText) || m_gameDataList == null)
            {
                return false;
            }

            GameData gameData = m_gameDataList[index];

            if (gameData.XBETitle != null && gameData.XBETitle.Length > 40 && ValidateFatX(gameData.XBETitle))
            {
                return false;
            }
            else if (gameData.XBETitleAlt != null && gameData.XBETitleAlt.Length > 40 && ValidateFatX(gameData.XBETitleAlt))
            {
                return false;
            }
            if (gameData.FolderName != null && gameData.FolderName.Length > 42 && ValidateFatX(gameData.FolderName))
            {
                return false;
            }
            else if (gameData.FolderNameAlt != null && gameData.FolderNameAlt.Length > 42 && ValidateFatX(gameData.FolderNameAlt))
            {
                return false;
            }
            else if (gameData.ISOName != null && gameData.ISOName.Length > 36 && ValidateFatX(gameData.ISOName))
            {
                return false;
            }
            else if (gameData.ISONameAlt != null && gameData.ISONameAlt.Length > 36 && ValidateFatX(gameData.ISONameAlt))
            {
                return false;
            }
            return true;
        }

        public void Run()
        {
            m_searchText = string.Empty;

            VeldridStartup.CreateWindowAndGraphicsDevice(new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, $"Repackinator - {m_version}"), new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true), GraphicsBackend.OpenGL, out m_window, out m_graphicsDevice);

            m_controller = new ImGuiController(m_graphicsDevice, m_graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, m_window.Width, m_window.Height);

            m_inputFolderPicker = new PathPicker
            {
                Mode = PathPicker.PickerMode.Folder
            };

            m_outputFolderPicker = new PathPicker
            {
                Mode = PathPicker.PickerMode.Folder
            };

            m_exportFolderPicker = new PathPicker
            {
                Mode = PathPicker.PickerMode.Folder,
                ButtonName = "Save"
            };

            m_editDialog = new EditDialog();
            m_okDialog = new OkDialog();
            m_creditsDialog = new CreditsDialog();
            m_repackDialog = new RepackDialog();
            m_scanDialog = new ScanDialog();

            m_showInvalid = false;

            m_config = Config.LoadConfig();

            m_gameDataList = GameDataHelper.LoadGameData();
            if (m_gameDataList == null)
            {
                m_gameDataList = Array.Empty<GameData>();
            }
            m_gameDataList = m_gameDataList.OrderBy(s => s.TitleName).ToArray();

            m_window.Resized += () =>
            {
                m_graphicsDevice.MainSwapchain.Resize((uint)m_window.Width, (uint)m_window.Height);
                m_controller.WindowResized(m_window.Width, m_window.Height);
            };

            m_commandList = m_graphicsDevice.ResourceFactory.CreateCommandList();

            while (m_window.Exists)
            {
                InputSnapshot snapshot = m_window.PumpEvents();
                if (!m_window.Exists)
                {
                    break;
                }
                m_controller.Update(1f / 60f, snapshot);

                RenderUI();

                m_commandList.Begin();
                m_commandList.SetFramebuffer(m_graphicsDevice.MainSwapchain.Framebuffer);
                m_commandList.ClearColorTarget(0, new RgbaFloat(0.0f, 0.0f, 0.0f, 1f));
                m_controller.Render(m_graphicsDevice, m_commandList);
                m_commandList.End();
                m_graphicsDevice.SubmitCommands(m_commandList);
                m_graphicsDevice.SwapBuffers(m_graphicsDevice.MainSwapchain);
            }

            m_graphicsDevice.WaitForIdle();
            m_controller.Dispose();
            m_commandList.Dispose();
            m_graphicsDevice.Dispose();
        }

        private void RenderUI()
        {
            if (m_window == null ||
                m_inputFolderPicker == null ||
                m_outputFolderPicker == null ||
                m_exportFolderPicker == null ||
                m_editDialog == null ||
                m_okDialog == null ||
                m_creditsDialog == null ||
                m_repackDialog == null ||
                m_scanDialog == null ||
                m_searchText == null ||
                m_gameDataList == null)
            {
                return;
            }

            if (m_inputFolderPicker.Render() && !m_inputFolderPicker.Cancelled)
            {
                m_config.InputPath = m_inputFolderPicker.SelectedFolder;
                Config.SaveConfig(m_config);
            }

            if (m_outputFolderPicker.Render() && !m_outputFolderPicker.Cancelled)
            {
                m_config.OutputPath = m_outputFolderPicker.SelectedFolder;
                Config.SaveConfig(m_config);
            }

            if (m_exportFolderPicker.Render() && !m_exportFolderPicker.Cancelled)
            {
                var exportFile = Path.Combine(m_exportFolderPicker.SelectedFolder, "Repackinator-Export.txt");
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(
                    "Title ID".PadRight(8) + " : " +
                    "Title Name".PadRight(40) + " : " +
                    "Version".PadRight(8) + " : " +
                    "Region".PadRight(30) + " : " +
                    "Letter".PadRight(6) + " : " +
                    "XBE Title".PadRight(40) + " : " +
                    "XBE Title Alt".PadRight(40) + " : " +
                    "Folder Name".PadRight(42) + " : " +
                    "Folder Name Alt".PadRight(42) + " : " +
                    "ISO Name".PadRight(36) + " : " +
                    "ISO Name Alt".PadRight(36) + " : " +
                    "ISO Checksum".PadRight(8));
                foreach (var item in m_gameDataList)
                {
                    if (!item.Selected)
                    {
                        continue;
                    }
                    stringBuilder.AppendLine($"{item.TitleID.PadRight(8)} : {item.TitleName.PadRight(40)} : {item.Version.PadRight(8)} : {item.Region.PadRight(30)} : {item.Letter.PadRight(6)} : {item.XBETitle.PadRight(40)} : {item.XBETitleAlt.PadRight(40)} : {item.FolderName.PadRight(42)} : {item.FolderNameAlt.PadRight(42)} : {item.ISOName.PadRight(36)} : {item.ISONameAlt.PadRight(36)} : {item.ISOChecksum.PadRight(8)}");
                }
                File.WriteAllText(exportFile, stringBuilder.ToString());
            }

            if (m_editDialog.Render())
            {
                m_gameDataList[m_editDialog.Index] = m_editDialog.GameData;
            }

            if (m_scanDialog.Render())
            {
                m_gameDataList = m_scanDialog.GameDataList;
            }

            m_okDialog.Render();
            m_creditsDialog.Render();
            m_repackDialog.Render();

            ImGui.Begin("Main", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize);
            ImGui.SetWindowSize(new Vector2(m_window.Width, m_window.Height));
            ImGui.SetWindowPos(new Vector2(0, 0), ImGuiCond.Always);

            string[] searchItems = new string[] { "Process", "Title ID", "Region", "Version", "Title Name", "Letter", "XBE Title", "XBE Title Alt", "Folder Name", "Folder Name Alt", "Iso Name", "Iso Name Alt", "Iso Checksum" };

            ImGui.Text("Search:");
            ImGui.PushItemWidth(200);
            if (ImGui.Combo("##searchField", ref m_searchField, searchItems, searchItems.Length))
            {
                m_searchText = string.Empty;
            }
            ImGui.PopItemWidth();
            ImGui.SameLine();
            ImGui.Text("for");
            ImGui.SameLine();
            ImGui.PushItemWidth(200);
            ImGui.InputText($"##searchText", ref m_searchText, 100);
            ImGui.PopItemWidth();

            ImGui.SameLine();
            ImGui.Checkbox("Show Invalid##showInvalid", ref m_showInvalid);

            ImGui.SameLine();
            ImGui.SetCursorPosX(m_window.Width - 240);
            ImGui.PushItemWidth(200);
            ImGui.ShowStyleSelector("Style");
            ImGui.PopItemWidth();

            ImGui.Spacing();

            const int MyItemColumnID_Process = 0;
            const int MyItemColumnID_Index = 1;
            const int MyItemColumnID_TitleID = 2;
            const int MyItemColumnID_Version = 3;
            const int MyItemColumnID_Region = 4;
            const int MyItemColumnID_TitleName = 5;
            const int MyItemColumnID_Letter = 6;
            const int MyItemColumnID_XBETitle = 7;
            const int MyItemColumnID_XBETitleAlt = 8;
            const int MyItemColumnID_FolderName = 9;
            const int MyItemColumnID_FolderNameAlt = 10;
            const int MyItemColumnID_IsoName = 11;
            const int MyItemColumnID_IsoNameAlt = 12;
            const int MyItemColumnID_IsoChecksum = 13;

            ImGuiTableFlags flags = ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Sortable | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg;
            if (ImGui.BeginTable("table_sorting", 14, flags, new Vector2(0.0f, m_window.Height - 234), 0.0f))
            {
                ImGui.TableSetupColumn("Process", ImGuiTableColumnFlags.WidthFixed, 75.0f, MyItemColumnID_Process);
                ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoSort, 75.0f, MyItemColumnID_Index);
                ImGui.TableSetupColumn("Title ID", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, 75.0f, MyItemColumnID_TitleID);
                ImGui.TableSetupColumn("Version", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, 75.0f, MyItemColumnID_Version);
                ImGui.TableSetupColumn("Region", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, 100.0f, MyItemColumnID_Region);
                ImGui.TableSetupColumn("Title Name", ImGuiTableColumnFlags.WidthFixed, 300.0f, MyItemColumnID_TitleName);
                ImGui.TableSetupColumn("Letter", ImGuiTableColumnFlags.WidthFixed, 75.0f, MyItemColumnID_Letter);
                ImGui.TableSetupColumn("XBE Title", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultSort, 300.0f, MyItemColumnID_XBETitle);
                ImGui.TableSetupColumn("XBE Title Alt", ImGuiTableColumnFlags.WidthFixed, 300.0f, MyItemColumnID_XBETitleAlt);
                ImGui.TableSetupColumn("Folder Name", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultSort, 300.0f, MyItemColumnID_FolderName);
                ImGui.TableSetupColumn("Folder Name Alt", ImGuiTableColumnFlags.WidthFixed, 300.0f, MyItemColumnID_FolderNameAlt);
                ImGui.TableSetupColumn("Iso Name", ImGuiTableColumnFlags.WidthFixed, 300.0f, MyItemColumnID_IsoName);
                ImGui.TableSetupColumn("Iso Name Alt", ImGuiTableColumnFlags.WidthFixed, 300.0f, MyItemColumnID_IsoNameAlt);
                ImGui.TableSetupColumn("Iso Checksum", ImGuiTableColumnFlags.WidthFixed, 100.0f, MyItemColumnID_IsoChecksum);
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                if (m_gameDataList != null)
                {
                    var sortSpects = ImGui.TableGetSortSpecs();
                    if (sortSpects.SpecsDirty)
                    {
                        var specsCount = sortSpects.SpecsCount;
                        if (specsCount == 1)
                        {
                            var specs = sortSpects.Specs;
                            var direction = specs.SortDirection;
                            var colIndex = specs.ColumnIndex;


                            if (colIndex == 0)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.Process) : m_gameDataList.OrderByDescending(s => s.Process)).ToArray();
                            }
                            else if (colIndex == 2)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.TitleID) : m_gameDataList.OrderByDescending(s => s.TitleID)).ToArray();
                            }
                            else if (colIndex == 3)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.Version) : m_gameDataList.OrderByDescending(s => s.Version)).ToArray();
                            }
                            else if (colIndex == 4)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.Region) : m_gameDataList.OrderByDescending(s => s.Region)).ToArray();
                            }
                            else if (colIndex == 5)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.TitleName) : m_gameDataList.OrderByDescending(s => s.TitleName)).ToArray();
                            }
                            else if (colIndex == 6)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.Letter) : m_gameDataList.OrderByDescending(s => s.Letter)).ToArray();
                            }
                            else if (colIndex == 7)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.XBETitle) : m_gameDataList.OrderByDescending(s => s.XBETitle)).ToArray();
                            }
                            else if (colIndex == 8)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.XBETitleAlt) : m_gameDataList.OrderByDescending(s => s.XBETitleAlt)).ToArray();
                            }
                            else if (colIndex == 9)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.FolderName) : m_gameDataList.OrderByDescending(s => s.FolderName)).ToArray();
                            }
                            else if (colIndex == 10)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.FolderNameAlt) : m_gameDataList.OrderByDescending(s => s.FolderNameAlt)).ToArray();
                            }
                            else if (colIndex == 11)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.ISOName) : m_gameDataList.OrderByDescending(s => s.ISOName)).ToArray();
                            }
                            else if (colIndex == 12)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.ISONameAlt) : m_gameDataList.OrderByDescending(s => s.ISONameAlt)).ToArray();
                            }
                            else if (colIndex == 13)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.ISOChecksum) : m_gameDataList.OrderByDescending(s => s.ISOChecksum)).ToArray();
                            }
                        }
                        sortSpects.SpecsDirty = false;
                    }

                    var textColor = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];

                    for (var i = 0; i < m_gameDataList.Length; i++)
                    {
                        if (IsFiltered(i))
                        {
                            continue;
                        }

                        if (m_showInvalid && !IsValidRow(i))
                        {
                            continue;
                        }

                        ImGui.PushID(i);
                        ImGui.TableNextRow(ImGuiTableRowFlags.None, 22);

                        ImGui.TableNextColumn();
                        bool process = string.Equals(m_gameDataList[i].Process, "Y", StringComparison.CurrentCultureIgnoreCase);
                        var colStartXProcess = ImGui.GetCursorPosX();
                        ImGui.SetCursorPosX(colStartXProcess + (((ImGui.GetColumnWidth()) - 20.0f) * 0.5f));
                        if (ImGui.Checkbox($"##process{i}", ref process))
                        {
                            m_gameDataList[i].Process = process ? "Y" : "N";
                        }

                        ImGui.TableNextColumn();
                        var textSizeIndex = ImGui.CalcTextSize((i + 1).ToString());
                        var colStartXIndex = ImGui.GetCursorPosX();
                        ImGui.SetCursorPosX(colStartXIndex + (((ImGui.GetColumnWidth()) - textSizeIndex.X) * 0.5f));
                        if (ImGui.Selectable((i + 1).ToString(), m_gameDataList[i].Selected, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick, new Vector2(0, 16)))
                        {
                            m_gameDataList[i].Selected = !m_gameDataList[i].Selected;
                            if (ImGui.IsMouseDoubleClicked(0))
                            {
                                m_editDialog.ShowModal(m_gameDataList[i], i);
                            }
                        }

                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(m_gameDataList[i].TitleID);
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(m_gameDataList[i].Version);
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(m_gameDataList[i].Region);
                        ImGui.TableNextColumn();
                        ImGui.TextUnformatted(m_gameDataList[i].TitleName);

                        ImGui.TableNextColumn();
                        string letter = m_gameDataList[i].Letter ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        ImGui.TextUnformatted(m_gameDataList[i].Letter);
                        ImGui.PopItemWidth();

                        ImGui.TableNextColumn();
                        string xbeTitle = m_gameDataList[i].XBETitle ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        ImGui.PushStyleColor(ImGuiCol.Text, xbeTitle.Length > 40 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.25f, 0.25f, 1)) : ImGui.ColorConvertFloat4ToU32(textColor));
                        ImGui.TextUnformatted(m_gameDataList[i].XBETitle);
                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();

                        ImGui.TableNextColumn();
                        string xbeTitleAlt = m_gameDataList[i].XBETitleAlt ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        ImGui.PushStyleColor(ImGuiCol.Text, xbeTitleAlt.Length > 40 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.25f, 0.25f, 1)) : ImGui.ColorConvertFloat4ToU32(textColor));
                        ImGui.TextUnformatted(m_gameDataList[i].XBETitleAlt);
                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();

                        ImGui.TableNextColumn();
                        string folderName = m_gameDataList[i].FolderName ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        ImGui.PushStyleColor(ImGuiCol.Text, folderName.Length > 42 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.25f, 0.25f, 1)) : ImGui.ColorConvertFloat4ToU32(textColor));
                        ImGui.TextUnformatted(m_gameDataList[i].FolderName);
                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();

                        ImGui.TableNextColumn();
                        string folderNameAlt = m_gameDataList[i].FolderNameAlt ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        ImGui.PushStyleColor(ImGuiCol.Text, folderNameAlt.Length > 42 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.25f, 0.25f, 1)) : ImGui.ColorConvertFloat4ToU32(textColor));
                        ImGui.TextUnformatted(m_gameDataList[i].FolderNameAlt);
                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();

                        ImGui.TableNextColumn();
                        string isoName = m_gameDataList[i].ISOName ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        ImGui.PushStyleColor(ImGuiCol.Text, isoName.Length > 36 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.25f, 0.25f, 1)) : ImGui.ColorConvertFloat4ToU32(textColor));
                        ImGui.TextUnformatted(m_gameDataList[i].ISOName);
                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();

                        ImGui.TableNextColumn();
                        string isoNameAlt = m_gameDataList[i].ISONameAlt ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        ImGui.PushStyleColor(ImGuiCol.Text, isoNameAlt.Length > 36 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.25f, 0.25f, 1)) : ImGui.ColorConvertFloat4ToU32(textColor));
                        ImGui.TextUnformatted(m_gameDataList[i].ISONameAlt);
                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();

                        ImGui.TableNextColumn();
                        string isoChecksum = m_gameDataList[i].ISOChecksum ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        ImGui.TextUnformatted(m_gameDataList[i].ISOChecksum);
                        ImGui.PopItemWidth();

                        ImGui.PopID();
                    }
                    ImGui.EndTable();
                }

            }

            string[] processItems = new string[] { "", "All", "None", "Inverse" };

            ImGui.Text("Process Selection:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(125);
            ImGui.PushItemWidth(100);
            if (ImGui.Combo("##processField", ref m_processField, processItems, processItems.Length))
            {
                if (m_gameDataList != null)
                {
                    for (var i = 0; i < m_gameDataList.Length; i++)
                    {
                        if (IsFiltered(i))
                        {
                            continue;
                        }
                        if (m_processField == 1)
                        {
                            m_gameDataList[i].Process = "Y";
                        }
                        else if (m_processField == 2)
                        {
                            m_gameDataList[i].Process = "N";
                        }
                        else if (m_processField == 3)
                        {
                            m_gameDataList[i].Process = string.Equals(m_gameDataList[i].Process, "Y", StringComparison.CurrentCultureIgnoreCase) ? "N" : "Y";
                        }
                    }
                    m_processField = 0;
                }
            }
            ImGui.PopItemWidth();

            ImGui.Spacing();

            string[] groupingItems = new string[] { "Default", "Region", "Letter", "Region Letter", "Letter Region" };

            ImGui.Text("Grouping Selection:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(125);
            ImGui.PushItemWidth(100);
            int grouping = (int)m_config.Grouping;
            if (ImGui.Combo("##groupingField", ref grouping, groupingItems, groupingItems.Length))
            {
                m_config.Grouping = (GroupingEnum)grouping;
                Config.SaveConfig(m_config);
            }
            ImGui.PopItemWidth();

            ImGui.Text("Use Alternate:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(125);
            bool alternative = m_config.Alternative;
            if (ImGui.Checkbox($"##alternate", ref alternative))
            {
                m_config.Alternative = alternative;
                Config.SaveConfig(m_config);
            }

            ImGui.Text("Input Folder:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(125);
            ImGui.PushItemWidth(400);
            string inputPath = m_config.InputPath;
            if (ImGui.InputText("##inputFolder", ref inputPath, 260))
            {
                m_config.InputPath = inputPath;
                Config.SaveConfig(m_config);
            }
            ImGui.PopItemWidth();
            ImGui.SameLine();
            if (ImGui.Button("...##inputPicker", new Vector2(30, 21)))
            {
                m_inputFolderPicker.ShowModal(Directory.GetCurrentDirectory());
            }

            ImGui.Text("Output Folder:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(125);
            ImGui.PushItemWidth(400);
            string outputPath = m_config.OutputPath;
            if (ImGui.InputText("##outputFolder", ref outputPath, 260))
            {
                m_config.OutputPath = outputPath;
                Config.SaveConfig(m_config);
            }
            ImGui.PopItemWidth();
            ImGui.SameLine();
            if (ImGui.Button("...##outputPicker", new Vector2(30, 21)))
            {
                m_outputFolderPicker.ShowModal(Directory.GetCurrentDirectory());
            }

            ImGui.SetCursorPosY(m_window.Height - 40);

            if (ImGui.Button("Save Game Data", new Vector2(100, 30)))
            {
                GameDataHelper.SaveGameData(m_gameDataList);
                m_okDialog.Title = "Saved";
                m_okDialog.Message = "Game data list has been saved.";
                m_okDialog.ShowModal();
            }

            ImGui.SameLine();

            if (ImGui.Button("Save Selected", new Vector2(100, 30)))
            {
                var applicationPath = Utility.GetApplicationPath();
                if (applicationPath != null)
                {
                    m_exportFolderPicker.ShowModal(applicationPath);
                }
            }

            ImGui.SameLine();

            if (ImGui.Button("Scan Output", new Vector2(100, 30)))
            {
                m_scanDialog.ShowModal(m_config, m_gameDataList);
            }

            ImGui.SameLine();

            if (ImGui.Button("Process", new Vector2(100, 30)))
            {
                if (!Directory.Exists(m_config.InputPath))
                {
                    m_okDialog.Title = "Error";
                    m_okDialog.Message = "Input folder is invalid.";
                    m_okDialog.ShowModal();
                }
                else if (!Directory.Exists(m_config.OutputPath))
                {
                    m_okDialog.Title = "Error";
                    m_okDialog.Message = "Output folder is invalid.";
                    m_okDialog.ShowModal();
                }
                else
                {
                    m_repackDialog.ShowModal(m_config, m_gameDataList);
                }
            }

            var message = "Coded by EqUiNoX - Team Resurgent";
            var messageSize = ImGui.CalcTextSize(message);
            ImGui.SetCursorPos(new Vector2(m_window.Width - messageSize.X - 10, m_window.Height - messageSize.Y - 10));
            ImGui.Text(message);
            ImGui.SetCursorPos(new Vector2(m_window.Width - messageSize.X - 10, m_window.Height - messageSize.Y - 10));
            if (ImGui.InvisibleButton("##credits", messageSize))
            {
                m_creditsDialog.ShowModal();
            }

            ImGui.End();
        }
    }
}
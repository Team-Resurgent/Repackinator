using ImGuiNET;
using Repackinator.Shared;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
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
        private AttachUpdateDialog? m_attachUpdateDialog;
        private Config m_config = new Config();

        private string? m_searchText;
        private int m_processField;
        private int m_scrubField;
        private bool m_showInvalid;
        private string m_version;
        private bool m_splitterDragBegin;
        private int m_splitterOffset = 0;
        private int m_splitterMouseY;
        private int m_splitterDragOffset = 0;

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

            if (m_config.SearchField == 0 && gameData.Process != null)
            {
                return !gameData.Process.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_config.SearchField == 1 && gameData.Scrub != null)
            {
                return !gameData.Scrub.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_config.SearchField == 2 && gameData.TitleID != null)
            {
                return !gameData.TitleID.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_config.SearchField == 3 && gameData.Region != null)
            {
                return !gameData.Region.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_config.SearchField == 4 && gameData.Version != null)
            {
                return !gameData.Version.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_config.SearchField == 5 && gameData.TitleName != null)
            {
                return !gameData.TitleName.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_config.SearchField == 6 && gameData.Letter != null)
            {
                return !gameData.Letter.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_config.SearchField == 7 && gameData.XBETitle != null)
            {
                return !gameData.XBETitle.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_config.SearchField == 8 && gameData.FolderName != null)
            {
                return !gameData.FolderName.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_config.SearchField == 9 && gameData.ISOName != null)
            {
                return !gameData.ISOName.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_config.SearchField == 10 && gameData.ISOChecksum != null)
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
            if (gameData.FolderName != null && gameData.FolderName.Length > 42 && ValidateFatX(gameData.FolderName))
            {
                return false;
            }
            else if (gameData.ISOName != null && gameData.ISOName.Length > 36 && ValidateFatX(gameData.ISOName))
            {
                return false;
            }
            return true;
        }

        private static void SetXboxTheme()
        {
            ImGui.StyleColorsDark();
            var style = ImGui.GetStyle();
            var colors = style.Colors;
            colors[(int)ImGuiCol.Text] = new Vector4(0.94f, 0.94f, 0.94f, 1.00f);
            colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.86f, 0.93f, 0.89f, 0.28f);
            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.10f, 0.10f, 0.10f, 1.00f);            
            colors[(int)ImGuiCol.ChildBg] = new Vector4(0.06f, 0.06f, 0.06f, 0.98f);
            colors[(int)ImGuiCol.PopupBg] = new Vector4(0.10f, 0.10f, 0.10f, 1.00f);
            colors[(int)ImGuiCol.Border] = new Vector4(0.11f, 0.11f, 0.11f, 0.60f);
            colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.16f, 0.16f, 0.16f, 0.00f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.18f, 0.18f, 0.18f, 1.00f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.30f, 0.30f, 0.30f, 1.00f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.28f, 0.71f, 0.25f, 1.00f);
            colors[(int)ImGuiCol.TitleBg] = new Vector4(0.20f, 0.51f, 0.18f, 1.00f);
            colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.26f, 0.66f, 0.23f, 1.00f);
            colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.16f, 0.16f, 0.16f, 0.75f);
            colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.14f, 0.14f, 0.14f, 0.00f);
            colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.16f, 0.16f, 0.16f, 0.00f);
            colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.30f, 0.30f, 0.30f, 1.00f);
            colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.24f, 0.60f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.24f, 0.60f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.CheckMark] = new Vector4(0.26f, 0.66f, 0.23f, 1.00f);
            colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.90f, 0.90f, 0.90f, 1.00f);
            colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
            colors[(int)ImGuiCol.Button] = new Vector4(0.17f, 0.17f, 0.17f, 1.00f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.24f, 0.60f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.26f, 0.66f, 0.23f, 1.00f);
            colors[(int)ImGuiCol.Header] = new Vector4(0.24f, 0.60f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.24f, 0.60f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.24f, 0.60f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.Separator] = new Vector4(1.00f, 1.00f, 1.00f, 0.25f);
            colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.13f, 0.87f, 0.16f, 0.78f);
            colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.25f, 0.75f, 0.10f, 1.00f);
            colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.47f, 0.83f, 0.49f, 0.04f);
            colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.28f, 0.71f, 0.25f, 0.78f);
            colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.28f, 0.71f, 0.25f, 1.00f);
            colors[(int)ImGuiCol.Tab] = new Vector4(0.26f, 0.67f, 0.23f, 0.95f);
            colors[(int)ImGuiCol.TabHovered] = new Vector4(0.24f, 0.60f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.TabActive] = new Vector4(0.24f, 0.60f, 0.00f, 1.00f);
            colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.21f, 0.54f, 0.19f, 0.99f);
            colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.24f, 0.60f, 0.21f, 1.00f);
            colors[(int)ImGuiCol.PlotLines] = new Vector4(0.86f, 0.93f, 0.89f, 0.63f);
            colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(0.28f, 0.71f, 0.25f, 1.00f);
            colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.86f, 0.93f, 0.89f, 0.63f);
            colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(0.28f, 0.71f, 0.25f, 1.00f);
            colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.26f, 0.66f, 0.23f, 1.00f);
            colors[(int)ImGuiCol.DragDropTarget] = new Vector4(1.00f, 1.00f, 0.00f, 0.90f);
            colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.28f, 0.71f, 0.25f, 1.00f);
            colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
            colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.80f, 0.80f, 0.80f, 0.20f);
            colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.16f, 0.16f, 0.16f, 0.73f);

            style.WindowRounding = 6;
            style.FrameRounding = 6;
            style.PopupRounding = 6;
        }

        private static void DrawToggle(bool enabled, bool hovered, Vector2 pos, Vector2 size)
        {
            var drawList = ImGui.GetWindowDrawList();

            float radius = size.Y * 0.5f;
            float rounding = size.Y * 0.25f;
            float slotHalfHeight = size.Y * 0.5f;

            var background = hovered ? ImGui.GetColorU32(enabled ? ImGuiCol.FrameBgActive : ImGuiCol.FrameBgHovered) : ImGui.GetColorU32(enabled ? ImGuiCol.CheckMark : ImGuiCol.FrameBg);

            var paddingMid = new Vector2(pos.X + radius + (enabled ? 1 : 0) * (size.X - radius * 2), pos.Y + size.Y / 2);
            var sizeMin = new Vector2(pos.X, paddingMid.Y - slotHalfHeight);
            var sizeMax = new Vector2(pos.X + size.X, paddingMid.Y + slotHalfHeight);
            drawList.AddRectFilled(sizeMin, sizeMax, background, rounding);

            var offs = new Vector2(radius*0.8f, radius * 0.8f);
            drawList.AddRectFilled(paddingMid - offs, paddingMid + offs, ImGui.GetColorU32(ImGuiCol.SliderGrab), rounding);
        }

        private static bool Toggle(string str_id, ref bool v, Vector2 size)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(new Vector4()));

            var style = ImGui.GetStyle();

            ImGui.PushID(str_id);
            bool status = ImGui.Button("###toggle_button", size);
            if (status)
            {
                v = !v;
            }
            ImGui.PopID();

            var maxRect = ImGui.GetItemRectMax();
            var toggleSize = new Vector2(size.X - 8, size.Y - 8);
            var togglePos = new Vector2(maxRect.X - toggleSize.X - style.FramePadding.X, maxRect.Y - toggleSize.Y - style.FramePadding.Y);
            DrawToggle(v, ImGui.IsItemHovered(), togglePos, toggleSize);

            ImGui.PopStyleColor();

            return status;
        }

        public void Run()
        {
            m_searchText = string.Empty;

            VeldridStartup.CreateWindowAndGraphicsDevice(new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, $"Repackinator - {m_version}"), new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true), VeldridStartup.GetPlatformDefaultBackend(), out m_window, out m_graphicsDevice);

            m_controller = new ImGuiController(m_graphicsDevice, m_graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, m_window.Width, m_window.Height);

            SetXboxTheme();

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

            m_editDialog = new();
            m_okDialog = new();
            m_creditsDialog = new();
            m_repackDialog = new();
            m_scanDialog = new();
            m_attachUpdateDialog = new();

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
                m_attachUpdateDialog == null ||
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
                    "Folder Name".PadRight(42) + " : " +
                    "ISO Name".PadRight(36) + " : " +
                    "ISO Checksum".PadRight(8));
                foreach (var item in m_gameDataList)
                {
                    if (!item.Selected)
                    {
                        continue;
                    }
                    stringBuilder.AppendLine($"{item.TitleID.PadRight(8)} : {item.TitleName.PadRight(40)} : {item.Version.PadRight(8)} : {item.Region.PadRight(30)} : {item.Letter.PadRight(6)} : {item.XBETitle.PadRight(40)} : {item.FolderName.PadRight(42)} : {item.ISOName.PadRight(36)} : {item.ISOChecksum.PadRight(8)}");
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
            m_attachUpdateDialog.Render();

            ImGui.Begin("Main", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize);
            ImGui.SetWindowSize(new Vector2(m_window.Width, m_window.Height));
            ImGui.SetWindowPos(new Vector2(0, 0), ImGuiCond.Always);

            string[] searchItems = new string[] { "Process", "Scrub", "Title ID", "Region", "Version", "Title Name", "Letter", "XBE Title", "Folder Name", "Iso Name", "Iso Checksum" };

            ImGui.Text("Search:");
            ImGui.PushItemWidth(200);
            var searchField = m_config.SearchField;
            if (ImGui.Combo("##searchField", ref searchField, searchItems, searchItems.Length))
            {
                m_searchText = string.Empty;
                m_config.SearchField = searchField;
                Config.SaveConfig(m_config);
            }
            ImGui.PopItemWidth();
            ImGui.SameLine();
            ImGui.Text("for");
            ImGui.SameLine();
            ImGui.PushItemWidth(200);
            ImGui.InputText($"##searchText", ref m_searchText, 100);
            ImGui.PopItemWidth();

            ImGui.SameLine();
            ImGui.Text("Show Invalid:");
            ImGui.SameLine();
            Toggle("##compress", ref m_showInvalid, new Vector2(38, 20));

            ImGui.Spacing();

            const int MyItemColumnID_Process = 0;
            const int MyItemColumnID_Scrub = 1;
            const int MyItemColumnID_Index = 2;             
            const int MyItemColumnID_TitleID = 3;
            const int MyItemColumnID_Version = 4;
            const int MyItemColumnID_Region = 5;
            const int MyItemColumnID_TitleName = 6;
            const int MyItemColumnID_Letter = 7;
            const int MyItemColumnID_XBETitle = 8;
            const int MyItemColumnID_FolderName = 9;
            const int MyItemColumnID_IsoName = 10;
            const int MyItemColumnID_IsoChecksum = 11;

            ImGuiTableFlags flags = ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Sortable | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg;
            if (ImGui.BeginTable("table_sorting", 12, flags, new Vector2(0.0f, m_window.Height - (340 + m_splitterOffset)), 0.0f))
            {
                ImGui.TableSetupColumn("Process", ImGuiTableColumnFlags.WidthFixed, 75.0f, MyItemColumnID_Process);
                ImGui.TableSetupColumn("Scrub", ImGuiTableColumnFlags.WidthFixed, 75.0f, MyItemColumnID_Scrub);
                ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoSort, 75.0f, MyItemColumnID_Index);
                ImGui.TableSetupColumn("Title ID", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, 75.0f, MyItemColumnID_TitleID);
                ImGui.TableSetupColumn("Version", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, 75.0f, MyItemColumnID_Version);
                ImGui.TableSetupColumn("Region", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, 100.0f, MyItemColumnID_Region);
                ImGui.TableSetupColumn("Title Name", ImGuiTableColumnFlags.WidthFixed, 300.0f, MyItemColumnID_TitleName);
                ImGui.TableSetupColumn("Letter", ImGuiTableColumnFlags.WidthFixed, 75.0f, MyItemColumnID_Letter);
                ImGui.TableSetupColumn("XBE Title", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultSort, 300.0f, MyItemColumnID_XBETitle);                
                ImGui.TableSetupColumn("Folder Name", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultSort, 300.0f, MyItemColumnID_FolderName);                
                ImGui.TableSetupColumn("Iso Name", ImGuiTableColumnFlags.WidthFixed, 300.0f, MyItemColumnID_IsoName);
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
                            if (colIndex == 1)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.Scrub) : m_gameDataList.OrderByDescending(s => s.Process)).ToArray();
                            }
                            else if (colIndex == 3)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.TitleID) : m_gameDataList.OrderByDescending(s => s.TitleID)).ToArray();
                            }
                            else if (colIndex == 4)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.Version) : m_gameDataList.OrderByDescending(s => s.Version)).ToArray();
                            }
                            else if (colIndex == 5)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.Region) : m_gameDataList.OrderByDescending(s => s.Region)).ToArray();
                            }
                            else if (colIndex == 6)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.TitleName) : m_gameDataList.OrderByDescending(s => s.TitleName)).ToArray();
                            }
                            else if (colIndex == 7)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.Letter) : m_gameDataList.OrderByDescending(s => s.Letter)).ToArray();
                            }
                            else if (colIndex == 8)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.XBETitle) : m_gameDataList.OrderByDescending(s => s.XBETitle)).ToArray();
                            }
                            else if (colIndex == 9)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.FolderName) : m_gameDataList.OrderByDescending(s => s.FolderName)).ToArray();
                            }
                            else if (colIndex == 10)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.ISOName) : m_gameDataList.OrderByDescending(s => s.ISOName)).ToArray();
                            }
                            else if (colIndex == 11)
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
                        if (Toggle($"##process{i}", ref process, new Vector2(38, 20)))
                        {
                            m_gameDataList[i].Process = process ? "Y" : "N";
                        }

                        ImGui.TableNextColumn();
                        bool scrub = string.Equals(m_gameDataList[i].Scrub, "Y", StringComparison.CurrentCultureIgnoreCase);
                        var colStartXScrub = ImGui.GetCursorPosX();
                        ImGui.SetCursorPosX(colStartXScrub + (((ImGui.GetColumnWidth()) - 20.0f) * 0.5f));
                        if (Toggle($"##scrub{i}", ref scrub, new Vector2(38, 20)))
                        {
                            m_gameDataList[i].Scrub = scrub ? "Y" : "N";
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
                        string folderName = m_gameDataList[i].FolderName ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        ImGui.PushStyleColor(ImGuiCol.Text, folderName.Length > 42 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.25f, 0.25f, 1)) : ImGui.ColorConvertFloat4ToU32(textColor));
                        ImGui.TextUnformatted(m_gameDataList[i].FolderName);
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
                        string isoChecksum = m_gameDataList[i].ISOChecksum ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        ImGui.TextUnformatted(m_gameDataList[i].ISOChecksum);
                        ImGui.PopItemWidth();

                        ImGui.PopID();
                    }
                    ImGui.EndTable();
                }

            }

            ImGui.Spacing();

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

            ImGui.SameLine();

            string[] scrubItems = new string[] { "", "All", "None", "Inverse" };

            ImGui.SetCursorPosX(250);

            ImGui.Text("Scrub Selection:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(350);
            ImGui.PushItemWidth(100);
            if (ImGui.Combo("##scrubField", ref m_scrubField, scrubItems, scrubItems.Length))
            {
                if (m_gameDataList != null)
                {
                    for (var i = 0; i < m_gameDataList.Length; i++)
                    {
                        if (IsFiltered(i))
                        {
                            continue;
                        }
                        if (m_scrubField == 1)
                        {
                            m_gameDataList[i].Scrub = "Y";
                        }
                        else if (m_scrubField == 2)
                        {
                            m_gameDataList[i].Scrub = "N";
                        }
                        else if (m_scrubField == 3)
                        {
                            m_gameDataList[i].Scrub = string.Equals(m_gameDataList[i].Scrub, "Y", StringComparison.CurrentCultureIgnoreCase) ? "N" : "Y";
                        }
                    }
                    m_scrubField = 0;
                }
            }
            ImGui.PopItemWidth();

            ImGui.Spacing();

            ImGui.Separator();
            ImGui.Selectable("", m_splitterDragBegin, ImGuiSelectableFlags.None, new Vector2(m_window.Width - 18, 6));
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                if (ImGui.IsItemHovered())
                {
                    if (m_splitterDragBegin == false)
                    {
                        m_splitterDragBegin = true;
                        m_splitterDragOffset = m_splitterOffset;
                        m_splitterMouseY = (int)ImGui.GetMousePos().Y;
                    }
                }
                if (m_splitterDragBegin)
                {
                    var mouseDiffY = m_splitterMouseY - (int)ImGui.GetMousePos().Y;
                    m_splitterOffset = Math.Max(Math.Min(m_splitterDragOffset + mouseDiffY, 0), -130);
                }
            }
            else if (m_splitterDragBegin == true)
            {
                m_splitterDragBegin = false;
            }
            ImGui.Separator();

            ImGui.Spacing();
  
            ImGui.Text("Config:");

            ImGui.BeginChild(3, new Vector2(m_window.Width - 16, 162 + m_splitterOffset), true, ImGuiWindowFlags.AlwaysUseWindowPadding);

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

            ImGui.Spacing();

            ImGui.Text("Use Uppercase:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(125);
            bool uppercase = m_config.UpperCase;
            if (Toggle("##uppercase", ref uppercase, new Vector2(38, 24)))
            {
                m_config.UpperCase = uppercase;
                Config.SaveConfig(m_config);
            }

            ImGui.Spacing();

            ImGui.Text("Compress:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(125);
            bool compress = m_config.Compress;
            if (Toggle("##compress", ref compress, new Vector2(38, 24)))
            {
                m_config.Compress = compress;
                Config.SaveConfig(m_config);
            }

            ImGui.Spacing();

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

            ImGui.Spacing();

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

            ImGui.EndChild();

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

            if (ImGui.Button("Attach Update", new Vector2(100, 30)))
            {
                m_attachUpdateDialog.ShowModal(m_config);
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

            ImGui.SameLine();

            ImGui.SetCursorPosX(m_window.Width - 216);
            if (ImGui.Button("Coded by EqUiNoX - Team Resurgent", new Vector2(208, 30)))
            {
                m_creditsDialog.ShowModal();
            }

            ImGui.End();
        }
    }
}

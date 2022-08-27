using ImGuiNET;
using Repackinator.Shared;
using SharpDX.D3DCompiler;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

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
        private PathPicker? m_tempFolderPicker;
        private OkDialog? m_okDialog;
        private RepackDialog? m_repackDialog;
        private Config m_config = new Config();

        private int m_searchField;
        private string? m_searchText;
        private int m_processField;
        private bool m_showInvalid;
        
        private bool IsFiltered(int index)
        {
            if (string.IsNullOrEmpty(m_searchText) || m_gameDataList == null || m_gameDataList[index] == null)
            {
                return false;
            }

            GameData gameData = m_gameDataList[index];

            if (m_searchField == 0 && gameData.TitleID != null)
            {
                return !gameData.TitleID.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 1 && gameData.Region != null)
            {
                return !gameData.Region.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 2 && gameData.TitleName != null)
            {
                return !gameData.TitleName.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 3 && gameData.Letter != null)
            {
                return !gameData.Letter.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 4 && gameData.XBETitleAndFolderName != null)
            {
                return !gameData.XBETitleAndFolderName.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 5 && gameData.XBETitleAndFolderNameAlt != null)
            {
                return !gameData.XBETitleAndFolderNameAlt.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 6 && gameData.ISOName != null)
            {
                return !gameData.ISOName.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (m_searchField == 7 && gameData.ISONameAlt != null)
            {
                return !gameData.ISONameAlt.Contains(m_searchText, StringComparison.CurrentCultureIgnoreCase);
            }
            return false;
        }

        private bool IsValidRow(int index)
        {
            if (string.IsNullOrEmpty(m_searchText) || m_gameDataList == null || m_gameDataList[index] == null)
            {
                return false;
            }

            GameData gameData = m_gameDataList[index];

            if (gameData.XBETitleAndFolderName != null && gameData.XBETitleAndFolderName.Length > 40)
            {
                return false;
            }
            else if (gameData.XBETitleAndFolderNameAlt != null && gameData.XBETitleAndFolderNameAlt.Length > 40)
            {
                return false;
            }
            else if (gameData.ISOName != null && gameData.ISOName.Length > 36)
            {
                return false;
            }
            else if (gameData.ISONameAlt != null && gameData.ISONameAlt.Length > 36)
            {
                return false;
            }
            return true;
        }

        public void Run()
        {
            //File.Delete("imgui.ini");

            m_searchText = string.Empty;

            VeldridStartup.CreateWindowAndGraphicsDevice(new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "Repackinator"), new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true), out m_window, out m_graphicsDevice);

            m_controller = new ImGuiController(m_graphicsDevice, m_graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, m_window.Width, m_window.Height);

            m_inputFolderPicker = new PathPicker
            {
                Mode = PathPicker.PickerMode.Folder
            };

            m_outputFolderPicker = new PathPicker
            {
                Mode = PathPicker.PickerMode.Folder
            };

            m_tempFolderPicker = new PathPicker
            {
                Mode = PathPicker.PickerMode.Folder
            };

            m_okDialog = new OkDialog();
            m_repackDialog = new RepackDialog();

            m_showInvalid = false;

            m_config = Config.LoadConfig();
          
            m_gameDataList = GameDataHelper.LoadGameData();
            if (m_gameDataList == null)
            {
                return;
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
                m_tempFolderPicker == null || 
                m_okDialog == null || 
                m_repackDialog == null || 
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

            if (m_tempFolderPicker.Render() && !m_tempFolderPicker.Cancelled)
            {
                m_config.TempPath = m_tempFolderPicker.SelectedFolder;
                Config.SaveConfig(m_config);
            }

            m_okDialog.Render();     
            m_repackDialog.Render();

            ImGui.Begin("Main", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize);
            ImGui.SetWindowSize(new Vector2(m_window.Width, m_window.Height));
            ImGui.SetWindowPos(new Vector2(0, 0), ImGuiCond.Always);

            string[] searchItems = new string[] { "Title ID", "Region", "Title Name", "Letter", "XBE Title And Folder Name", "XBE Title And Folder Name Alt", "Iso Name", "Iso Name Alt" };
         
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

            ImGui.Spacing();

            const int MyItemColumnID_Process = 0;
            const int MyItemColumnID_TitleID = 1;            
            const int MyItemColumnID_Version = 2;
            const int MyItemColumnID_Region = 3;
            const int MyItemColumnID_TitleName = 4;
            const int MyItemColumnID_Letter = 5;
            const int MyItemColumnID_XBETitleAndFolderName = 6;
            const int MyItemColumnID_XBETitleAndFolderNameAlt = 7;
            const int MyItemColumnID_IsoName = 8;
            const int MyItemColumnID_IsoNameAlt = 9;

            ImGuiTableFlags flags = ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Sortable | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg;
            if (ImGui.BeginTable("table_sorting", 10, flags, new Vector2(0.0f, m_window.Height - 240), 0.0f))
            {
                ImGui.TableSetupColumn("Process", ImGuiTableColumnFlags.WidthFixed, 75.0f, MyItemColumnID_Process);
                ImGui.TableSetupColumn("Title ID", ImGuiTableColumnFlags.WidthFixed, 75.0f, MyItemColumnID_TitleID);
                ImGui.TableSetupColumn("Version", ImGuiTableColumnFlags.WidthFixed, 75.0f, MyItemColumnID_Version);
                ImGui.TableSetupColumn("Region", ImGuiTableColumnFlags.WidthFixed, 100.0f, MyItemColumnID_Region);
                ImGui.TableSetupColumn("Title Name", ImGuiTableColumnFlags.WidthFixed, 300.0f, MyItemColumnID_TitleName);
                ImGui.TableSetupColumn("Letter", ImGuiTableColumnFlags.WidthFixed, 75.0f, MyItemColumnID_Letter);
                ImGui.TableSetupColumn("XBE Title And Folder Name", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultSort, 300.0f, MyItemColumnID_XBETitleAndFolderName);
                ImGui.TableSetupColumn("XBE Title And Folder Name Alt", ImGuiTableColumnFlags.WidthFixed, 300.0f, MyItemColumnID_XBETitleAndFolderNameAlt);
                ImGui.TableSetupColumn("Iso Name", ImGuiTableColumnFlags.WidthFixed, 300.0f, MyItemColumnID_IsoName);
                ImGui.TableSetupColumn("Iso Name Alt", ImGuiTableColumnFlags.WidthFixed, 300.0f, MyItemColumnID_IsoNameAlt);
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
                                m_gameDataList =  (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.Process) : m_gameDataList.OrderByDescending(s => s.Process)).ToArray();
                            }
                            else if (colIndex == 1)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.TitleID) : m_gameDataList.OrderByDescending(s => s.TitleID)).ToArray();
                            }
                            else if (colIndex == 2)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.Version) : m_gameDataList.OrderByDescending(s => s.Version)).ToArray();
                            }
                            else if (colIndex == 3)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.Region) : m_gameDataList.OrderByDescending(s => s.Region)).ToArray();
                            }
                            else if (colIndex == 4)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.TitleName) : m_gameDataList.OrderByDescending(s => s.TitleName)).ToArray();
                            }
                            else if (colIndex == 5)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.Letter) : m_gameDataList.OrderByDescending(s => s.Letter)).ToArray();                                
                            }
                            else if (colIndex == 6)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.XBETitleAndFolderName) : m_gameDataList.OrderByDescending(s => s.XBETitleAndFolderName)).ToArray();
                            }
                            else if (colIndex == 7)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.XBETitleAndFolderNameAlt) : m_gameDataList.OrderByDescending(s => s.XBETitleAndFolderNameAlt)).ToArray();
                            }
                            else if (colIndex == 8)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.ISOName) : m_gameDataList.OrderByDescending(s => s.ISOName)).ToArray();
                            }
                            else if (colIndex == 9)
                            {
                                m_gameDataList = (specs.SortDirection == ImGuiSortDirection.Ascending ? m_gameDataList.OrderBy(s => s.ISONameAlt) : m_gameDataList.OrderByDescending(s => s.ISONameAlt)).ToArray();
                            }
                        }
                        sortSpects.SpecsDirty = false;
                    }

                    for (var i = 0; i < m_gameDataList.Length; i++)
                    {
                        if (m_gameDataList[i] == null || IsFiltered(i))
                        {
                            continue;
                        }

                        if (m_showInvalid && !IsValidRow(i))
                        {
                            continue;
                        }

                        ImGui.PushID(i);
                        ImGui.TableNextRow();
                        
                        ImGui.TableNextColumn();
                        bool process = string.Equals(m_gameDataList[i].Process, "Y", StringComparison.CurrentCultureIgnoreCase);
                        ImGui.SetCursorPosX(((ImGui.GetColumnWidth()) - 15.0f) * 0.5f);
                        if (ImGui.Checkbox($"##process{i}", ref process))
                        {
                            m_gameDataList[i].Process = process ? "Y" : "N";
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
                        ImGui.PushItemWidth(75.0f);
                        if (ImGui.InputText($"##letter{i}", ref letter, 1))
                        {
                            m_gameDataList[i].Letter = letter;
                        }
                        ImGui.PopItemWidth();

                        ImGui.TableNextColumn();
                        string xbeTitleAndFolderName = m_gameDataList[i].XBETitleAndFolderName ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        ImGui.PushStyleColor(ImGuiCol.Text, xbeTitleAndFolderName.Length > 40 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.5f, 0.5f, 1)) : ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)));
                        if (ImGui.InputText($"##xbeTitleAndFolderName{i}", ref xbeTitleAndFolderName, 40))
                        {
                            m_gameDataList[i].XBETitleAndFolderName = xbeTitleAndFolderName;
                        }
                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();

                        ImGui.TableNextColumn();
                        string xbeTitleAndFolderNameAlt = m_gameDataList[i].XBETitleAndFolderNameAlt ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        ImGui.PushStyleColor(ImGuiCol.Text, xbeTitleAndFolderNameAlt.Length > 40 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.5f, 0.5f, 1)) : ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)));
                        if (ImGui.InputText($"##xbeTitleAndFolderNameAlt{i}", ref xbeTitleAndFolderNameAlt, 40))
                        {
                            m_gameDataList[i].XBETitleAndFolderNameAlt = xbeTitleAndFolderNameAlt;
                        }
                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();

                        ImGui.TableNextColumn();
                        string isoName = m_gameDataList[i].ISOName ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        ImGui.PushStyleColor(ImGuiCol.Text, isoName.Length > 36 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.5f, 0.5f, 1)) : ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)));
                        if (ImGui.InputText($"##isoName{i}", ref isoName, 36))
                        {
                            m_gameDataList[i].ISOName = isoName;
                        }
                        ImGui.PopStyleColor();
                        ImGui.PopItemWidth();

                        ImGui.TableNextColumn();
                        string isoNameAlt = m_gameDataList[i].ISONameAlt ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        ImGui.PushStyleColor(ImGuiCol.Text, isoNameAlt.Length > 36 ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.5f, 0.5f, 1)) : ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)));
                        if (ImGui.InputText($"##isoNameAlt{i}", ref isoNameAlt, 36))
                        {
                            m_gameDataList[i].ISONameAlt = isoNameAlt;
                        }
                        ImGui.PopStyleColor();
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
                        if (m_gameDataList[i] == null || IsFiltered(i))
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
            ImGui.InputText("##inputFolder", ref inputPath, 260);
            m_config.InputPath = inputPath;
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
            ImGui.InputText("##outputFolder", ref outputPath, 260);
            m_config.OutputPath = outputPath;
            ImGui.PopItemWidth();
            ImGui.SameLine();
            if (ImGui.Button("...##outputPicker", new Vector2(30, 21)))
            {
                m_outputFolderPicker.ShowModal(Directory.GetCurrentDirectory());
            }

            ImGui.Text("Temp Folder:");
            ImGui.SameLine();
            ImGui.SetCursorPosX(125);
            ImGui.PushItemWidth(400);
            string tempPath = m_config.TempPath;
            ImGui.InputText("##tenpFolder", ref tempPath, 260);
            m_config.TempPath = tempPath; 
            ImGui.PopItemWidth();
            ImGui.SameLine();
            if (ImGui.Button("...##tenpPicker", new Vector2(30, 21)))
            {
                m_tempFolderPicker.ShowModal(Directory.GetCurrentDirectory());
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

            if (ImGui.Button("Process", new Vector2(100, 30)))
            {
                //if (!Directory.Exists(m_inputFolder))
                //{
                //    m_okDialog.Title = "Error";
                //    m_okDialog.Message = "Input folder is invalid.";
                //    m_okDialog.ShowModal();
                //}
                //else if (!Directory.Exists(m_outputFolder))
                //{
                //    m_okDialog.Title = "Error";
                //    m_okDialog.Message = "Output folder is invalid.";
                //    m_okDialog.ShowModal();
                //}
                //else if (!Directory.Exists(m_tempFolder))
                //{
                //    m_okDialog.Title = "Error";
                //    m_okDialog.Message = "Temp folder is invalid.";
                //    m_okDialog.ShowModal();
                //}
                //else
                {
                    m_repackDialog.ShowModal();
                }
            }

            var message = "Coded by EqUiNoX";
            var messageSize = ImGui.CalcTextSize(message);
            ImGui.SetCursorPos(new Vector2(m_window.Width - messageSize.X - 10, m_window.Height - messageSize.Y - 10));            
            ImGui.Text(message);
            ImGui.End();          
        }
    }
}
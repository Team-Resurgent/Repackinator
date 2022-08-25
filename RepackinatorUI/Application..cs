using ImGuiNET;
using Repackinator.Shared;
using SharpDX.D3DCompiler;
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
        private static Sdl2Window? m_window;
        private static GraphicsDevice? m_graphicsDevice;
        private static CommandList? m_commandList;
        private static ImGuiController? m_controller;
        private static GameData[]? m_gameDataList;
        private static PathPicker? m_folderPicker;
        private static PathPicker? m_filePicker;

        public static void Run()
        {
            //File.Delete("imgui.ini");

            VeldridStartup.CreateWindowAndGraphicsDevice(new WindowCreateInfo(50, 50, 1280, 720, WindowState.Normal, "Repackinator"), new GraphicsDeviceOptions(true, null, true, ResourceBindingModel.Improved, true, true), out m_window, out m_graphicsDevice);

            m_controller = new ImGuiController(m_graphicsDevice, m_graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, m_window.Width, m_window.Height);

            m_folderPicker = new PathPicker
            {
                Mode = PathPicker.PickerMode.Folder
            };

            m_filePicker = new PathPicker
            {
                Mode = PathPicker.PickerMode.File
            };

            m_gameDataList = GameData.LoadGameData();
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

        private static void RenderUI()
        {
            if (m_window == null || m_folderPicker == null || m_filePicker == null)
            {
                return;
            }

            if (m_folderPicker.Render() && !m_folderPicker.Cancelled)
            {
                var path = m_folderPicker.SelectedFolder;
            }

            if (m_filePicker.Render() && !m_folderPicker.Cancelled)
            {
                var path = m_filePicker.SelectedFile;
            }

            ImGui.Begin("Main", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize);
            ImGui.SetWindowSize(new Vector2(m_window.Width, m_window.Height));
            ImGui.SetWindowPos(new Vector2(0, 0), ImGuiCond.Always);

            ImGui.Text("Search goes here");

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
            
            const int TEXT_BASE_HEIGHT = 20;
     
            ImGuiTableFlags flags = ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Sortable | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg;
            if (ImGui.BeginTable("table_sorting", 10, flags, new Vector2(0.0f, TEXT_BASE_HEIGHT * 15), 0.0f))
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

                    // Files have a max length of 42 chars
                    // Xbe Title name length = 40 chars
                    // Xbe File name length - extension (.xbe) = 38 chars
                    // Iso File name length - extension (.x.iso) = 36 chars

                    for (int i = 0; i < m_gameDataList.Length; i++)
                    {
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
                        if (ImGui.InputText($"##xbeTitleAndFolderName{i}", ref xbeTitleAndFolderName, 38))
                        {
                            m_gameDataList[i].XBETitleAndFolderName = xbeTitleAndFolderName;
                        }
                        ImGui.PopItemWidth();

                        ImGui.TableNextColumn();
                        string xbeTitleAndFolderNameAlt = m_gameDataList[i].XBETitleAndFolderNameAlt ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        if (ImGui.InputText($"##xbeTitleAndFolderNameAlt{i}", ref xbeTitleAndFolderNameAlt, 38))
                        {
                            m_gameDataList[i].XBETitleAndFolderNameAlt = xbeTitleAndFolderNameAlt;
                        }
                        ImGui.PopItemWidth();

                        ImGui.TableNextColumn();
                        string isoName = m_gameDataList[i].ISOName ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        if (ImGui.InputText($"##isoName{i}", ref isoName, 36))
                        {
                            m_gameDataList[i].ISOName = isoName;
                        }
                        ImGui.PopItemWidth();

                        ImGui.TableNextColumn();
                        string isoNameAlt = m_gameDataList[i].ISONameAlt ?? "";
                        ImGui.PushItemWidth(ImGui.GetColumnWidth());
                        if (ImGui.InputText($"##isoNameAlt{i}", ref isoNameAlt, 36))
                        {
                            m_gameDataList[i].ISONameAlt = isoNameAlt;
                        }
                        ImGui.PopItemWidth();

                        ImGui.PopID();
                    }
                    ImGui.EndTable();
                }

            }

            ImGui.Text("Some file picker demos...");

            if (ImGui.Button("Folder Picker", new Vector2(100, 30)))
            {
                m_folderPicker.ShowModal(Directory.GetCurrentDirectory());
            }

            if (ImGui.Button("File Picker", new Vector2(100, 30)))
            {
                m_filePicker.ShowModal(Directory.GetCurrentDirectory());
            }

            ImGui.Spacing();
            ImGui.Text("Coded by EqUiNoX");
            ImGui.End();
          


            //ImGui.Begin("Main2");

            //// 3. Show the ImGui demo window. Most of the sample code is in ImGui.ShowDemoWindow(). Read its code to learn more about Dear ImGui!
            //if (_showImGuiDemoWindow)
            //{
            //    // Normally user code doesn't need/want to call this because positions are saved in .ini file anyway.
            //    // Here we just want to make the demo initial state a bit more friendly!
            //    ImGui.SetNextWindowPos(new Vector2(650, 20), ImGuiCond.FirstUseEver);
            //    ImGui.ShowDemoWindow(ref _showImGuiDemoWindow);
            //}

            //if (ImGui.TreeNode("Tabs"))
            //{
            //    if (ImGui.TreeNode("Basic"))
            //    {
            //        ImGuiTabBarFlags tab_bar_flags = ImGuiTabBarFlags.None;
            //        if (ImGui.BeginTabBar("MyTabBar", tab_bar_flags))
            //        {
            //            if (ImGui.BeginTabItem("Avocado"))
            //            {
            //                ImGui.Text("This is the Avocado tab!\nblah blah blah blah blah");
            //                ImGui.EndTabItem();
            //            }
            //            if (ImGui.BeginTabItem("Broccoli"))
            //            {
            //                ImGui.Text("This is the Broccoli tab!\nblah blah blah blah blah");
            //                ImGui.EndTabItem();
            //            }
            //            if (ImGui.BeginTabItem("Cucumber"))
            //            {
            //                ImGui.Text("This is the Cucumber tab!\nblah blah blah blah blah");
            //                ImGui.EndTabItem();
            //            }
            //            ImGui.EndTabBar();
            //        }
            //        ImGui.Separator();
            //        ImGui.TreePop();
            //    }

            //    if (ImGui.TreeNode("Advanced & Close Button"))
            //    {
            //        // Expose a couple of the available flags. In most cases you may just call BeginTabBar() with no flags (0).
            //        ImGui.CheckboxFlags("ImGuiTabBarFlags_Reorderable", ref s_tab_bar_flags, (uint)ImGuiTabBarFlags.Reorderable);
            //        ImGui.CheckboxFlags("ImGuiTabBarFlags_AutoSelectNewTabs", ref s_tab_bar_flags, (uint)ImGuiTabBarFlags.AutoSelectNewTabs);
            //        ImGui.CheckboxFlags("ImGuiTabBarFlags_NoCloseWithMiddleMouseButton", ref s_tab_bar_flags, (uint)ImGuiTabBarFlags.NoCloseWithMiddleMouseButton);
            //        if ((s_tab_bar_flags & (uint)ImGuiTabBarFlags.FittingPolicyMask) == 0)
            //            s_tab_bar_flags |= (uint)ImGuiTabBarFlags.FittingPolicyDefault;
            //        if (ImGui.CheckboxFlags("ImGuiTabBarFlags_FittingPolicyResizeDown", ref s_tab_bar_flags, (uint)ImGuiTabBarFlags.FittingPolicyResizeDown))
            //            s_tab_bar_flags &= ~((uint)ImGuiTabBarFlags.FittingPolicyMask ^ (uint)ImGuiTabBarFlags.FittingPolicyResizeDown);
            //        if (ImGui.CheckboxFlags("ImGuiTabBarFlags_FittingPolicyScroll", ref s_tab_bar_flags, (uint)ImGuiTabBarFlags.FittingPolicyScroll))
            //            s_tab_bar_flags &= ~((uint)ImGuiTabBarFlags.FittingPolicyMask ^ (uint)ImGuiTabBarFlags.FittingPolicyScroll);

            //        // Tab Bar
            //        string[] names = { "Artichoke", "Beetroot", "Celery", "Daikon" };

            //        for (int n = 0; n < s_opened.Length; n++)
            //        {
            //            if (n > 0) { ImGui.SameLine(); }
            //            ImGui.Checkbox(names[n], ref s_opened[n]);
            //        }

            //        // Passing a bool* to BeginTabItem() is similar to passing one to Begin(): the underlying bool will be set to false when the tab is closed.
            //        if (ImGui.BeginTabBar("MyTabBar", (ImGuiTabBarFlags)s_tab_bar_flags))
            //        {
            //            for (int n = 0; n < s_opened.Length; n++)
            //                if (s_opened[n] && ImGui.BeginTabItem(names[n], ref s_opened[n]))
            //                {
            //                    ImGui.Text($"This is the {names[n]} tab!");
            //                    if ((n & 1) != 0)
            //                        ImGui.Text("I am an odd tab.");
            //                    ImGui.EndTabItem();
            //                }
            //            ImGui.EndTabBar();
            //        }
            //        ImGui.Separator();
            //        ImGui.TreePop();
            //    }
            //    ImGui.TreePop();
            //}

            //ImGuiIOPtr io = ImGui.GetIO();
            //SetThing(out io.DeltaTime, 2f);


            //ImGui.End();
        }
    }
}
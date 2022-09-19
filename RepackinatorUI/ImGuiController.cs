using ImGuiNET;
using Repackinator.Shared;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;

namespace RepackinatorUI
{
    public class ImGuiController : IDisposable
    {
        private GraphicsDevice _gd;
        private bool _frameBegun;

        private DeviceBuffer? _vertexBuffer;
        private DeviceBuffer? _indexBuffer;
        private DeviceBuffer? _projMatrixBuffer;
        private Texture? _fontTexture;
        private TextureView? _fontTextureView;
        private Shader? _vertexShader;
        private Shader? _fragmentShader;
        private ResourceLayout? _layout;
        private ResourceLayout? _textureLayout;
        private Pipeline? _pipeline;
        private ResourceSet? _mainResourceSet;
        private ResourceSet? _fontTextureResourceSet;

        private IntPtr _fontAtlasID = (IntPtr)1;
        private IntPtr _iniNamePtr = IntPtr.Zero;
        private bool _controlDown;
        private bool _shiftDown;
        private bool _altDown;
        private bool _winKeyDown;

        private int _windowWidth;
        private int _windowHeight;
        private Vector2 _scaleFactor = Vector2.One;

        private readonly Dictionary<TextureView, ResourceSetInfo> _setsByView = new();
        private readonly Dictionary<Texture, TextureView> _autoViewsByTexture = new();
        private readonly Dictionary<IntPtr, ResourceSetInfo> _viewsById = new();
        private readonly List<IDisposable> _ownedResources = new();
        private int _lastAssignedID = 100;

        private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            IntPtr libHandle = IntPtr.Zero;
            if (libraryName == "cimgui")
            {
                if (Environment.Is64BitProcess)
                {
                    libHandle = NativeLibrary.Load(@"x64\cimgui.dll");
                }
                else
                {
                    libHandle = NativeLibrary.Load(@"x86\cimgui.dll");
                }
            }
            return libHandle;
        }

        private static unsafe void InitFonts()
        {
            var fontData = ResourceLoader.GetEmbeddedResourceBytes("ARIALUNI.TTF", typeof(ImGuiController).GetTypeInfo().Assembly);
            var pinnedFontData = GCHandle.Alloc(fontData, GCHandleType.Pinned);
            IntPtr addressFontData = pinnedFontData.AddrOfPinnedObject();

            ImFontConfigPtr fontConfig = ImGuiNative.ImFontConfig_ImFontConfig();
            fontConfig.PixelSnapH = true;
            fontConfig.OversampleH = 3;
            fontConfig.OversampleV = 3;
            fontConfig.RasterizerMultiply = 1f;
            fontConfig.MergeMode = false;

            var glyphRangeHandle1 = GCHandle.Alloc(new ushort[] {
                0x0020, 0x00FF, // Basic Latin + Latin Supplement
                0x2000, 0x206F, // General Punctuation
                0x2100, 0x214f, // Letter Like Symbols
                0x3000, 0x30FF, // CJK Symbols and Punctuations, Hiragana, Katakana
                0x31F0, 0x31FF, // Katakana Phonetic Extensions
                0xFF00, 0xFFEF, // Half-width characters
                0xFFFD, 0xFFFD, // Invalid
                0x4e00, 0x9FAF, // CJK Ideograms
                0
            }, GCHandleType.Pinned);


            ImGui.GetIO().Fonts.AddFontFromMemoryTTF(addressFontData, fontData.Length, 15, null, glyphRangeHandle1.AddrOfPinnedObject());

            var glyphRangeHandle2 = GCHandle.Alloc(new ushort[] {
                0x0020, 0x00FF, // Basic Latin + Latin Supplement
                0x2000, 0x206F, // General Punctuation               
                0
            }, GCHandleType.Pinned);

            ImGui.GetIO().Fonts.AddFontFromMemoryTTF(addressFontData, fontData.Length, 30, fontConfig, glyphRangeHandle2.AddrOfPinnedObject());

            ImGui.GetIO().Fonts.Build();

            pinnedFontData.Free();
            glyphRangeHandle1.Free();
            glyphRangeHandle2.Free();
            ImGuiNative.ImFontConfig_destroy(fontConfig);
        }

        public unsafe ImGuiController(GraphicsDevice gd, OutputDescription outputDescription, int width, int height)
        {
            _gd = gd;
            _windowWidth = width;
            _windowHeight = height;

            //NativeLibrary.SetDllImportResolver(typeof(ImGui).Assembly, ImportResolver);

            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);

            InitFonts();

            _iniNamePtr = Marshal.StringToCoTaskMemUTF8("settings.ini");
            ImGui.GetIO().NativePtr->IniFilename = (byte*)_iniNamePtr;

            ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

            CreateDeviceResources(gd, outputDescription);
            SetKeyMappings();

            SetPerFrameImGuiData(1f / 60f);

            ImGui.NewFrame();
            _frameBegun = true;
        }

        public void WindowResized(int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;
        }

        public void DestroyDeviceObjects()
        {
            Dispose();
        }

        public void CreateDeviceResources(GraphicsDevice gd, OutputDescription outputDescription)
        {
            _gd = gd;

            ResourceFactory factory = gd.ResourceFactory;
            _vertexBuffer = factory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            _vertexBuffer.Name = "ImGui.NET Vertex Buffer";
            _indexBuffer = factory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            _indexBuffer.Name = "ImGui.NET Index Buffer";
            RecreateFontDeviceTexture(gd);

            _projMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

            byte[] vertexShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-vertex", ShaderStages.Vertex);
            byte[] fragmentShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-frag", ShaderStages.Fragment);
            _vertexShader = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexShaderBytes, gd.BackendType == GraphicsBackend.Metal ? "VS" : "main"));
            _fragmentShader = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderBytes, gd.BackendType == GraphicsBackend.Metal ? "FS" : "main"));

            VertexLayoutDescription[] vertexLayouts = new VertexLayoutDescription[]
            {
                new VertexLayoutDescription(new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm))
            };

            _layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
            _textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));

            GraphicsPipelineDescription pd = new(
                BlendStateDescription.SingleAlphaBlend,
                new DepthStencilStateDescription(false, false, ComparisonKind.Always),
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, true),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(vertexLayouts, new[] { _vertexShader, _fragmentShader }),
                new ResourceLayout[] { _layout, _textureLayout },
                outputDescription,
                ResourceBindingModel.Default);
            _pipeline = factory.CreateGraphicsPipeline(ref pd);
            _mainResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_layout, _projMatrixBuffer, gd.PointSampler));
            _fontTextureResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_textureLayout, _fontTextureView));
        }

        public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, TextureView textureView)
        {
            if (!_setsByView.TryGetValue(textureView, out ResourceSetInfo rsi))
            {
                var resourceSet = factory.CreateResourceSet(new ResourceSetDescription(_textureLayout, textureView));
                rsi = new ResourceSetInfo(GetNextImGuiBindingID(), resourceSet);

                _setsByView.Add(textureView, rsi);
                _viewsById.Add(rsi.ImGuiBinding, rsi);
                _ownedResources.Add(resourceSet);
            }

            return rsi.ImGuiBinding;
        }

        private IntPtr GetNextImGuiBindingID()
        {
            int newID = _lastAssignedID++;
            return (IntPtr)newID;
        }

        public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture)
        {
            if (!_autoViewsByTexture.TryGetValue(texture, out TextureView? textureView))
            {
                textureView = factory.CreateTextureView(texture);
                _autoViewsByTexture.Add(texture, textureView);
                _ownedResources.Add(textureView);
            }

            return GetOrCreateImGuiBinding(factory, textureView);
        }

        public ResourceSet GetImageResourceSet(IntPtr imGuiBinding)
        {
            if (!_viewsById.TryGetValue(imGuiBinding, out ResourceSetInfo tvi))
            {
                throw new InvalidOperationException("No registered ImGui binding with id " + imGuiBinding.ToString());
            }

            return tvi.ResourceSet;
        }

        public void ClearCachedImageResources()
        {
            foreach (IDisposable resource in _ownedResources)
            {
                resource.Dispose();
            }

            _ownedResources.Clear();
            _setsByView.Clear();
            _viewsById.Clear();
            _autoViewsByTexture.Clear();
            _lastAssignedID = 100;
        }

        private static byte[] LoadEmbeddedShaderCode(ResourceFactory factory, string name, ShaderStages stage)
        {
            switch (factory.BackendType)
            {
                case GraphicsBackend.Direct3D11:
                    {
                        string resourceName = name + ".hlsl.bytes";
                        return GetEmbeddedResourceBytes(resourceName);
                    }
                case GraphicsBackend.OpenGL:
                    {
                        string resourceName = name + ".glsl";
                        return GetEmbeddedResourceBytes(resourceName);
                    }
                case GraphicsBackend.Vulkan:
                    {
                        string resourceName = name + ".spv";
                        return GetEmbeddedResourceBytes(resourceName);
                    }
                case GraphicsBackend.Metal:
                    {
                        string resourceName = name + ".metallib";
                        return GetEmbeddedResourceBytes(resourceName);
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private static byte[] GetEmbeddedResourceBytes(string resourceName)
        {
            Assembly assembly = typeof(ImGuiController).Assembly;
            if (assembly == null)
            {
                return Array.Empty<byte>();
            }
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return Array.Empty<byte>();
            }
            byte[] ret = new byte[stream.Length];
            stream.Read(ret, 0, (int)stream.Length);
            return ret;
        }

        public void RecreateFontDeviceTexture(GraphicsDevice gd)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            // Build
            IntPtr pixels;
            int width, height, bytesPerPixel;
            io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height, out bytesPerPixel);
            // Store our identifier
            io.Fonts.SetTexID(_fontAtlasID);

            _fontTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            _fontTexture.Name = "ImGui.NET Font Texture";
            gd.UpdateTexture(_fontTexture, pixels, (uint)(bytesPerPixel * width * height), 0, 0, 0, (uint)width, (uint)height, 1, 0, 0);
            _fontTextureView = gd.ResourceFactory.CreateTextureView(_fontTexture);

            io.Fonts.ClearTexData();
        }

        public void Render(GraphicsDevice gd, CommandList cl)
        {
            if (_frameBegun)
            {
                _frameBegun = false;
                ImGui.Render();
                RenderImDrawData(ImGui.GetDrawData(), gd, cl);
            }
        }

        public void Update(float deltaSeconds, InputSnapshot snapshot)
        {
            if (_frameBegun)
            {
                ImGui.Render();
            }

            SetPerFrameImGuiData(deltaSeconds);
            UpdateImGuiInput(snapshot);

            _frameBegun = true;
            ImGui.NewFrame();
        }

        private void SetPerFrameImGuiData(float deltaSeconds)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new Vector2(
                _windowWidth / _scaleFactor.X,
                _windowHeight / _scaleFactor.Y);
            io.DisplayFramebufferScale = _scaleFactor;
            io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
        }

        private void UpdateImGuiInput(InputSnapshot snapshot)
        {
            ImGuiIOPtr io = ImGui.GetIO();

            Vector2 mousePosition = snapshot.MousePosition;

            // Determine if any of the mouse buttons were pressed during this snapshot period, even if they are no longer held.
            bool leftPressed = false;
            bool middlePressed = false;
            bool rightPressed = false;
            foreach (MouseEvent me in snapshot.MouseEvents)
            {
                if (me.Down)
                {
                    switch (me.MouseButton)
                    {
                        case MouseButton.Left:
                            leftPressed = true;
                            break;
                        case MouseButton.Middle:
                            middlePressed = true;
                            break;
                        case MouseButton.Right:
                            rightPressed = true;
                            break;
                    }
                }
            }

            io.MouseDown[0] = leftPressed || snapshot.IsMouseDown(MouseButton.Left);
            io.MouseDown[1] = rightPressed || snapshot.IsMouseDown(MouseButton.Right);
            io.MouseDown[2] = middlePressed || snapshot.IsMouseDown(MouseButton.Middle);
            io.MousePos = mousePosition;
            io.MouseWheel = snapshot.WheelDelta;

            IReadOnlyList<char> keyCharPresses = snapshot.KeyCharPresses;
            for (int i = 0; i < keyCharPresses.Count; i++)
            {
                char c = keyCharPresses[i];
                io.AddInputCharacter(c);
            }

            IReadOnlyList<KeyEvent> keyEvents = snapshot.KeyEvents;
            for (int i = 0; i < keyEvents.Count; i++)
            {
                KeyEvent keyEvent = keyEvents[i];
                io.KeysDown[(int)keyEvent.Key] = keyEvent.Down;
                if (keyEvent.Key == Key.ControlLeft)
                {
                    _controlDown = keyEvent.Down;
                }
                if (keyEvent.Key == Key.ShiftLeft)
                {
                    _shiftDown = keyEvent.Down;
                }
                if (keyEvent.Key == Key.AltLeft)
                {
                    _altDown = keyEvent.Down;
                }
                if (keyEvent.Key == Key.WinLeft)
                {
                    _winKeyDown = keyEvent.Down;
                }
            }

            io.KeyCtrl = _controlDown;
            io.KeyAlt = _altDown;
            io.KeyShift = _shiftDown;
            io.KeySuper = _winKeyDown;
        }

        private static void SetKeyMappings()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
            io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
            io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
            io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
            io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
            io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
            io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
            io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
            io.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
            io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
            io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.BackSpace;
            io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
            io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
            io.KeyMap[(int)ImGuiKey.Space] = (int)Key.Space;
            io.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
            io.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
            io.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
            io.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
            io.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
            io.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;
        }

        private void RenderImDrawData(ImDrawDataPtr draw_data, GraphicsDevice gd, CommandList cl)
        {
            if (_vertexBuffer == null ||
                _indexBuffer == null)
            {
                return;
            }

            uint vertexOffsetInVertices = 0;
            uint indexOffsetInElements = 0;

            if (draw_data.CmdListsCount == 0)
            {
                return;
            }

            uint totalVBSize = (uint)(draw_data.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
            if (totalVBSize > _vertexBuffer.SizeInBytes)
            {
                gd.DisposeWhenIdle(_vertexBuffer);
                _vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVBSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            }

            uint totalIBSize = (uint)(draw_data.TotalIdxCount * sizeof(ushort));
            if (totalIBSize > _indexBuffer.SizeInBytes)
            {
                gd.DisposeWhenIdle(_indexBuffer);
                _indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIBSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
            }

            for (int i = 0; i < draw_data.CmdListsCount; i++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[i];

                cl.UpdateBuffer(
                    _vertexBuffer,
                    vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(),
                    cmd_list.VtxBuffer.Data,
                    (uint)(cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));

                cl.UpdateBuffer(
                    _indexBuffer,
                    indexOffsetInElements * sizeof(ushort),
                    cmd_list.IdxBuffer.Data,
                    (uint)(cmd_list.IdxBuffer.Size * sizeof(ushort)));

                vertexOffsetInVertices += (uint)cmd_list.VtxBuffer.Size;
                indexOffsetInElements += (uint)cmd_list.IdxBuffer.Size;
            }

            // Setup orthographic projection matrix into our constant buffer
            ImGuiIOPtr io = ImGui.GetIO();
            Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(
                0f,
                io.DisplaySize.X,
                io.DisplaySize.Y,
                0.0f,
                -1.0f,
                1.0f);

            _gd.UpdateBuffer(_projMatrixBuffer, 0, ref mvp);

            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            cl.SetPipeline(_pipeline);
            cl.SetGraphicsResourceSet(0, _mainResourceSet);

            draw_data.ScaleClipRects(io.DisplayFramebufferScale);

            // Render command lists
            int vtx_offset = 0;
            int idx_offset = 0;
            for (int n = 0; n < draw_data.CmdListsCount; n++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdListsRange[n];
                for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
                {
                    ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        if (pcmd.TextureId != IntPtr.Zero)
                        {
                            if (pcmd.TextureId == _fontAtlasID)
                            {
                                cl.SetGraphicsResourceSet(1, _fontTextureResourceSet);
                            }
                            else
                            {
                                cl.SetGraphicsResourceSet(1, GetImageResourceSet(pcmd.TextureId));
                            }
                        }

                        cl.SetScissorRect(
                            0,
                            (uint)pcmd.ClipRect.X,
                            (uint)pcmd.ClipRect.Y,
                            (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                            (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                        cl.DrawIndexed(pcmd.ElemCount, 1, pcmd.IdxOffset + (uint)idx_offset, (int)pcmd.VtxOffset + vtx_offset, 0);
                    }
                }
                vtx_offset += cmd_list.VtxBuffer.Size;
                idx_offset += cmd_list.IdxBuffer.Size;
            }
        }

        public void Dispose()
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _projMatrixBuffer?.Dispose();
            _fontTexture?.Dispose();
            _fontTextureView?.Dispose();
            _vertexShader?.Dispose();
            _fragmentShader?.Dispose();
            _layout?.Dispose();
            _textureLayout?.Dispose();
            _pipeline?.Dispose();
            _mainResourceSet?.Dispose();

            foreach (IDisposable resource in _ownedResources)
            {
                resource.Dispose();
            }

            Marshal.ZeroFreeCoTaskMemUTF8(_iniNamePtr);
        }

        private struct ResourceSetInfo
        {
            public readonly IntPtr ImGuiBinding;
            public readonly ResourceSet ResourceSet;

            public ResourceSetInfo(IntPtr imGuiBinding, ResourceSet resourceSet)
            {
                ImGuiBinding = imGuiBinding;
                ResourceSet = resourceSet;
            }
        }
    }
}

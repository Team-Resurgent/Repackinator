using ImGuiNET;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using Repackinator.Helpers;
using SixLabors.ImageSharp.PixelFormats;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Repackinator.UI
{
    public class ImGuiController : IDisposable
    {
        private bool _disposed;

        private bool _frameBegun;

        private int _vertexArray;
        private int _vertexBuffer;
        private int _vertexBufferSize;
        private int _indexBuffer;
        private int _indexBufferSize;

        private int _fontTexture;

        private int _shader;
        private int _shaderFontTextureLocation;
        private int _shaderProjectionMatrixLocation;

        private static bool KHRDebugAvailable = false;

        private int _splashTexture;

        private int _windowWidth;
        private int _windowHeight;
        private Vector2 _hdpiScale = Vector2.One;
        private Vector2 _retinaScale = Vector2.One;

        public Vector2 GetScaleFactor()
        {
            return _hdpiScale / _retinaScale;
        }

        public int SplashTexture => _splashTexture;

        private IntPtr _iniNamePtr = IntPtr.Zero;
        private List<int> _ownedTextures = new List<int>();

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

        private unsafe int CreateTextureFromResource(string resourceName)
        {
            var resourceBytes = ResourceLoader.GetEmbeddedResourceBytes(resourceName);
            using var resourceImage = SixLabors.ImageSharp.Image.Load<Bgra32>(resourceBytes);
            var pixelSpan = new Span<Bgra32>(new Bgra32[resourceImage.Width * resourceImage.Height]);
            resourceImage.CopyPixelDataTo(pixelSpan);

            int mips = (int)Math.Floor(Math.Log(Math.Max(resourceImage.Width, resourceImage.Height), 2));

            int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);
            int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);

            var texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexStorage2D(TextureTarget2d.Texture2D, mips, SizedInternalFormat.Rgba8, resourceImage.Width, resourceImage.Height);
            LabelObject(ObjectLabelIdentifier.Texture, texture, resourceName);

            var byteSpan = MemoryMarshal.AsBytes(pixelSpan);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, resourceImage.Width, resourceImage.Height, PixelFormat.Bgra, PixelType.UnsignedByte, byteSpan.ToArray());

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, mips - 1);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

            GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
            GL.ActiveTexture((TextureUnit)prevActiveTexture);

            _ownedTextures.Add(texture);
            return texture;
        }

        public void InitImages()
        {
            _splashTexture = CreateTextureFromResource("Repackinator.Resources.TeamResurgent.jpg");
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

        public unsafe ImGuiController(Window window)
        {
            _windowWidth = window.ClientSize.X;
            _windowHeight = window.ClientSize.Y;

            _hdpiScale = Vector2.One;
            if (window.TryGetCurrentMonitorScale(out var scaleX, out var scaleY))
            {
                _hdpiScale = new Vector2(scaleX, scaleY);
            }

            _retinaScale = new Vector2(window.Width / (float)window.Size.X, window.Height / (float)window.Size.Y);

            int major = GL.GetInteger(GetPName.MajorVersion);
            int minor = GL.GetInteger(GetPName.MinorVersion);

            KHRDebugAvailable = (major == 4 && minor >= 3) || IsExtensionSupported("KHR_debug");

            //NativeLibrary.SetDllImportResolver(typeof(ImGui).Assembly, ImportResolver);

            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);

            InitFonts();

            _iniNamePtr = Marshal.StringToCoTaskMemUTF8("settings.ini");
            ImGui.GetIO().NativePtr->IniFilename = (byte*)_iniNamePtr;

            ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

            CreateDeviceResources();
            InitImages();

            SetPerFrameImGuiData(1 / 60f);

            SetXboxTheme();

            ImGui.NewFrame();
            _frameBegun = true;
        }

        public void WindowResized(int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;
        }

        public void CreateDeviceResources()
        {
            _vertexBufferSize = 10000;
            _indexBufferSize = 2000;

            int prevVAO = GL.GetInteger(GetPName.VertexArrayBinding);
            int prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);

            _vertexArray = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArray);
            LabelObject(ObjectLabelIdentifier.VertexArray, _vertexArray, "ImGui");

            _vertexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            LabelObject(ObjectLabelIdentifier.Buffer, _vertexBuffer, "VBO: ImGui");
            GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            _indexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            LabelObject(ObjectLabelIdentifier.Buffer, _indexBuffer, "EBO: ImGui");
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            RecreateFontDeviceTexture();

            string VertexSource = ResourceLoader.GetEmbeddedResourceString("Repackinator.Resources.imgui-vertex.glsl");
            string FragmentSource = ResourceLoader.GetEmbeddedResourceString("Repackinator.Resources.imgui-frag.glsl");

            _shader = CreateProgram("ImGui", VertexSource, FragmentSource);
            _shaderProjectionMatrixLocation = GL.GetUniformLocation(_shader, "projection_matrix");
            _shaderFontTextureLocation = GL.GetUniformLocation(_shader, "in_fontTexture");

            int stride = Unsafe.SizeOf<ImDrawVert>();
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            GL.BindVertexArray(prevVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);

            CheckGLError("End of ImGui setup");
        }

        public void RecreateFontDeviceTexture()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

            int mips = (int)Math.Floor(Math.Log(Math.Max(width, height), 2));

            int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);
            int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);

            _fontTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
            GL.TexStorage2D(TextureTarget2d.Texture2D, mips, SizedInternalFormat.Rgba8, width, height);
            LabelObject(ObjectLabelIdentifier.Texture, _fontTexture, "ImGui Text Atlas");

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, pixels);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, mips - 1);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

            GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
            GL.ActiveTexture((TextureUnit)prevActiveTexture);

            io.Fonts.SetTexID(_fontTexture);

            io.Fonts.ClearTexData();
        }

        public void Render()
        {
            if (_frameBegun)
            {
                _frameBegun = false;
                ImGui.Render();
                RenderImDrawData(ImGui.GetDrawData());
            }
        }

        public void Update(float deltaSeconds, GameWindow wnd)
        {
            if (_frameBegun)
            {
                ImGui.Render();
            }

            SetPerFrameImGuiData(deltaSeconds);
            UpdateImGuiInput(wnd);

            _frameBegun = true;
            ImGui.NewFrame();
        }

        private void SetPerFrameImGuiData(float deltaSeconds)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            io.DisplaySize = new Vector2(_windowWidth, _windowHeight) / _hdpiScale;
            io.DeltaTime = deltaSeconds;
        }

        readonly List<char> PressedChars = new List<char>();

        internal void PressChar(char keyChar)
        {
            PressedChars.Add(keyChar);
        }

        internal void MouseScroll(Vector2 offset)
        {
            ImGuiIOPtr io = ImGui.GetIO();

            io.MouseWheel = offset.Y;
            io.MouseWheelH = offset.X;
        }

        private ImGuiKey TranslateKey(Keys key)
        {
            if (key >= Keys.D0 && key <= Keys.D9)
                return key - Keys.D0 + ImGuiKey._0;

            if (key >= Keys.A && key <= Keys.Z)
                return key - Keys.A + ImGuiKey.A;

            if (key >= Keys.KeyPad0 && key <= Keys.KeyPad9)
                return key - Keys.KeyPad0 + ImGuiKey.Keypad0;

            if (key >= Keys.F1 && key <= Keys.F12)
                return key - Keys.F1 + ImGuiKey.F12;

            switch (key)
            {
                case Keys.Tab: return ImGuiKey.Tab;
                case Keys.Left: return ImGuiKey.LeftArrow;
                case Keys.Right: return ImGuiKey.RightArrow;
                case Keys.Up: return ImGuiKey.UpArrow;
                case Keys.Down: return ImGuiKey.DownArrow;
                case Keys.PageUp: return ImGuiKey.PageUp;
                case Keys.PageDown: return ImGuiKey.PageDown;
                case Keys.Home: return ImGuiKey.Home;
                case Keys.End: return ImGuiKey.End;
                case Keys.Insert: return ImGuiKey.Insert;
                case Keys.Delete: return ImGuiKey.Delete;
                case Keys.Backspace: return ImGuiKey.Backspace;
                case Keys.Space: return ImGuiKey.Space;
                case Keys.Enter: return ImGuiKey.Enter;
                case Keys.Escape: return ImGuiKey.Escape;
                case Keys.Apostrophe: return ImGuiKey.Apostrophe;
                case Keys.Comma: return ImGuiKey.Comma;
                case Keys.Minus: return ImGuiKey.Minus;
                case Keys.Period: return ImGuiKey.Period;
                case Keys.Slash: return ImGuiKey.Slash;
                case Keys.Semicolon: return ImGuiKey.Semicolon;
                case Keys.Equal: return ImGuiKey.Equal;
                case Keys.LeftBracket: return ImGuiKey.LeftBracket;
                case Keys.Backslash: return ImGuiKey.Backslash;
                case Keys.RightBracket: return ImGuiKey.RightBracket;
                case Keys.GraveAccent: return ImGuiKey.GraveAccent;
                case Keys.CapsLock: return ImGuiKey.CapsLock;
                case Keys.ScrollLock: return ImGuiKey.ScrollLock;
                case Keys.NumLock: return ImGuiKey.NumLock;
                case Keys.PrintScreen: return ImGuiKey.PrintScreen;
                case Keys.Pause: return ImGuiKey.Pause;
                case Keys.KeyPadDecimal: return ImGuiKey.KeypadDecimal;
                case Keys.KeyPadDivide: return ImGuiKey.KeypadDivide;
                case Keys.KeyPadMultiply: return ImGuiKey.KeypadMultiply;
                case Keys.KeyPadSubtract: return ImGuiKey.KeypadSubtract;
                case Keys.KeyPadAdd: return ImGuiKey.KeypadAdd;
                case Keys.KeyPadEnter: return ImGuiKey.KeypadEnter;
                case Keys.KeyPadEqual: return ImGuiKey.KeypadEqual;
                case Keys.LeftShift: return ImGuiKey.LeftShift;
                case Keys.LeftControl: return ImGuiKey.LeftCtrl;
                case Keys.LeftAlt: return ImGuiKey.LeftAlt;
                case Keys.LeftSuper: return ImGuiKey.LeftSuper;
                case Keys.RightShift: return ImGuiKey.RightShift;
                case Keys.RightControl: return ImGuiKey.RightCtrl;
                case Keys.RightAlt: return ImGuiKey.RightAlt;
                case Keys.RightSuper: return ImGuiKey.RightSuper;
                case Keys.Menu: return ImGuiKey.Menu;
                default: return ImGuiKey.None;
            }
        }


        private void UpdateImGuiInput(GameWindow wnd)
        {
            ImGuiIOPtr io = ImGui.GetIO();

            MouseState MouseState = wnd.MouseState;
            KeyboardState KeyboardState = wnd.KeyboardState;

            io.MouseDown[0] = MouseState[MouseButton.Left];
            io.MouseDown[1] = MouseState[MouseButton.Right];
            io.MouseDown[2] = MouseState[MouseButton.Middle];
            io.MouseDown[3] = MouseState[MouseButton.Button4];
            io.MouseDown[4] = MouseState[MouseButton.Button5];

            //var screenPoint = new Vector2i((int)MouseState.X, (int)MouseState.Y);
            //var point = screenPoint;//wnd.PointToClient(screenPoint);
            //io.MousePos = new System.Numerics.Vector2(point.X, point.Y);
            io.MousePos = new Vector2((int)MouseState.X, (int)MouseState.Y);// / GetScaleFactor();

            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                if (key == Keys.Unknown)
                {
                    continue;
                }
                io.AddKeyEvent(TranslateKey(key), KeyboardState.IsKeyDown(key));
            }

            foreach (var c in PressedChars)
            {
                io.AddInputCharacter(c);
            }
            PressedChars.Clear();

            io.KeyCtrl = KeyboardState.IsKeyDown(Keys.LeftControl) || KeyboardState.IsKeyDown(Keys.RightControl);
            io.KeyAlt = KeyboardState.IsKeyDown(Keys.LeftAlt) || KeyboardState.IsKeyDown(Keys.RightAlt);
            io.KeyShift = KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift);
            io.KeySuper = KeyboardState.IsKeyDown(Keys.LeftSuper) || KeyboardState.IsKeyDown(Keys.RightSuper);
        }

        private void RenderImDrawData(ImDrawDataPtr draw_data)
        {
            if (draw_data.CmdListsCount == 0)
            {
                return;
            }

            int prevVAO = GL.GetInteger(GetPName.VertexArrayBinding);
            int prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);
            int prevProgram = GL.GetInteger(GetPName.CurrentProgram);
            bool prevBlendEnabled = GL.GetBoolean(GetPName.Blend);
            bool prevScissorTestEnabled = GL.GetBoolean(GetPName.ScissorTest);
            int prevBlendEquationRgb = GL.GetInteger(GetPName.BlendEquationRgb);
            int prevBlendEquationAlpha = GL.GetInteger(GetPName.BlendEquationAlpha);
            int prevBlendFuncSrcRgb = GL.GetInteger(GetPName.BlendSrcRgb);
            int prevBlendFuncSrcAlpha = GL.GetInteger(GetPName.BlendSrcAlpha);
            int prevBlendFuncDstRgb = GL.GetInteger(GetPName.BlendDstRgb);
            int prevBlendFuncDstAlpha = GL.GetInteger(GetPName.BlendDstAlpha);
            bool prevCullFaceEnabled = GL.GetBoolean(GetPName.CullFace);
            bool prevDepthTestEnabled = GL.GetBoolean(GetPName.DepthTest);
            int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
            GL.ActiveTexture(TextureUnit.Texture0);
            int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);
            Span<int> prevScissorBox = stackalloc int[4];
            unsafe
            {
                fixed (int* iptr = &prevScissorBox[0])
                {
                    GL.GetInteger(GetPName.ScissorBox, iptr);
                }
            }

            // Bind the element buffer (thru the VAO) so that we can resize it.
            GL.BindVertexArray(_vertexArray);
            // Bind the vertex buffer so that we can resize it.
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            for (int i = 0; i < draw_data.CmdListsCount; i++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdLists[i];

                int vertexSize = cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
                if (vertexSize > _vertexBufferSize)
                {
                    int newSize = (int)Math.Max(_vertexBufferSize * 1.5f, vertexSize);

                    GL.BufferData(BufferTarget.ArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                    _vertexBufferSize = newSize;

                    Debug.WriteLine($"Resized dear imgui vertex buffer to new size {_vertexBufferSize}");
                }

                int indexSize = cmd_list.IdxBuffer.Size * sizeof(ushort);
                if (indexSize > _indexBufferSize)
                {
                    int newSize = (int)Math.Max(_indexBufferSize * 1.5f, indexSize);
                    GL.BufferData(BufferTarget.ElementArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                    _indexBufferSize = newSize;

                    Debug.WriteLine($"Resized dear imgui index buffer to new size {_indexBufferSize}");
                }
            }

            // Setup orthographic projection matrix into our constant buffer
            ImGuiIOPtr io = ImGui.GetIO();

            var mvp = OpenTK.Mathematics.Matrix4.CreateOrthographicOffCenter(0.0f, io.DisplaySize.X, io.DisplaySize.Y, 0.0f, -1.0f, 1.0f);

            GL.UseProgram(_shader);
            GL.UniformMatrix4(_shaderProjectionMatrixLocation, false, ref mvp);
            GL.Uniform1(_shaderFontTextureLocation, 0);
            CheckGLError("Projection");

            GL.BindVertexArray(_vertexArray);
            CheckGLError("VAO");

            draw_data.ScaleClipRects(_hdpiScale);

            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.ScissorTest);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);

            // Render command lists
            for (int n = 0; n < draw_data.CmdListsCount; n++)
            {
                ImDrawListPtr cmd_list = draw_data.CmdLists[n];

                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmd_list.VtxBuffer.Data);
                CheckGLError($"Data Vert {n}");

                GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, cmd_list.IdxBuffer.Size * sizeof(ushort), cmd_list.IdxBuffer.Data);
                CheckGLError($"Data Idx {n}");

                for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
                {
                    ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        GL.ActiveTexture(TextureUnit.Texture0);
                        GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);
                        CheckGLError("Texture");

                        // We do _windowHeight - (int)clip.W instead of (int)clip.Y because gl has flipped Y when it comes to these coordinates
                        var clip = pcmd.ClipRect;
                        GL.Scissor((int)clip.X, _windowHeight - (int)clip.W, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));
                        CheckGLError("Scissor");

                        if ((io.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                        {
                            GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(pcmd.IdxOffset * sizeof(ushort)), unchecked((int)pcmd.VtxOffset));
                        }
                        else
                        {
                            GL.DrawElements(BeginMode.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (int)pcmd.IdxOffset * sizeof(ushort));
                        }
                        CheckGLError("Draw");
                    }
                }
            }

            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.ScissorTest);

            // Reset state
            GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
            GL.ActiveTexture((TextureUnit)prevActiveTexture);
            GL.UseProgram(prevProgram);
            GL.BindVertexArray(prevVAO);
            GL.Scissor(prevScissorBox[0], prevScissorBox[1], prevScissorBox[2], prevScissorBox[3]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);
            GL.BlendEquationSeparate((BlendEquationMode)prevBlendEquationRgb, (BlendEquationMode)prevBlendEquationAlpha);
            GL.BlendFuncSeparate((BlendingFactorSrc)prevBlendFuncSrcRgb, (BlendingFactorDest)prevBlendFuncDstRgb, (BlendingFactorSrc)prevBlendFuncSrcAlpha, (BlendingFactorDest)prevBlendFuncDstAlpha);
            if (prevBlendEnabled) GL.Enable(EnableCap.Blend); else GL.Disable(EnableCap.Blend);
            if (prevDepthTestEnabled) GL.Enable(EnableCap.DepthTest); else GL.Disable(EnableCap.DepthTest);
            if (prevCullFaceEnabled) GL.Enable(EnableCap.CullFace); else GL.Disable(EnableCap.CullFace);
            if (prevScissorTestEnabled) GL.Enable(EnableCap.ScissorTest); else GL.Disable(EnableCap.ScissorTest);
        }

        public static void LabelObject(ObjectLabelIdentifier objLabelIdent, int glObject, string name)
        {
            if (KHRDebugAvailable)
            {
                GL.ObjectLabel(objLabelIdent, glObject, name.Length, name);
            }
        }

        static bool IsExtensionSupported(string name)
        {
            int n = GL.GetInteger(GetPName.NumExtensions);
            for (int i = 0; i < n; i++)
            {
                string extension = GL.GetString(StringNameIndexed.Extensions, i);
                if (extension == name) return true;
            }

            return false;
        }

        public static int CreateProgram(string name, string vertexSource, string fragmentSoruce)
        {
            int program = GL.CreateProgram();
            LabelObject(ObjectLabelIdentifier.Program, program, $"Program: {name}");

            int vertex = CompileShader(name, ShaderType.VertexShader, vertexSource);
            int fragment = CompileShader(name, ShaderType.FragmentShader, fragmentSoruce);

            GL.AttachShader(program, vertex);
            GL.AttachShader(program, fragment);

            GL.LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string info = GL.GetProgramInfoLog(program);
                Debug.WriteLine($"GL.LinkProgram had info log [{name}]:\n{info}");
            }

            GL.DetachShader(program, vertex);
            GL.DetachShader(program, fragment);

            GL.DeleteShader(vertex);
            GL.DeleteShader(fragment);

            return program;
        }

        private static int CompileShader(string name, ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            LabelObject(ObjectLabelIdentifier.Shader, shader, $"Shader: {name}");

            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string info = GL.GetShaderInfoLog(shader);
                System.Console.WriteLine($"Error: GL.CompileShader for shader '{name}' [{type}] had info log:\n{info}");
            }

            return shader;
        }

        public static void CheckGLError(string title)
        {
            OpenTK.Graphics.OpenGL.ErrorCode error;
            int i = 1;
            while ((error = GL.GetError()) != OpenTK.Graphics.OpenGL.ErrorCode.NoError)
            {
                System.Console.WriteLine($"Error: {title} ({i++}) - {error}");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Free unmanaged resources
                }
                GL.DeleteVertexArray(_vertexArray);
                GL.DeleteBuffer(_vertexBuffer);
                GL.DeleteBuffer(_indexBuffer);
                GL.DeleteTexture(_fontTexture);

                foreach (var texture in _ownedTextures)
                {
                    GL.DeleteTexture(texture);
                }

                GL.DeleteProgram(_shader);

                Marshal.ZeroFreeCoTaskMemUTF8(_iniNamePtr);

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}

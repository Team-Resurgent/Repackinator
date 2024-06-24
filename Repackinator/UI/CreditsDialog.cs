using ImGuiNET;
using ManagedBass.FftSignalProvider;
using ManagedBass;
using System.Numerics;
using System.Reflection;
using Repackinator.Helpers;
using Repackinator.Models;

namespace Repackinator.UI
{
    public class CreditsDialog
    {
        private struct Star
        {
            public Vector2 Pos { get; set; }
            public float Scale { get; set; }
        }

        private Config _config = new();
        private bool _showModal;
        private bool _open;

        ImFontPtr _font;

        private int m_playBackHandle;
        private SignalProvider? m_signalProvider;

        public string Title { get; set; } = "Credits";

        public string Message { get; set; } = "Repackinator brought to you by Team Resurgent and in collaboration with Team Cerbios, what application wouldn't be complete without some retro style credits... Repackinator is Open Source on our GitHub and is open to the Community to contribute to and help evolve the application into something amazing... Music is a remix of the C64 classic Comic Bakery by Stuart Wilson... Coding by EqUiNoX... Application Design by EqUiNoX, HoRnEyDvL, Hazeno... Testing by HoRnEyDvL, Hazeno, Rocky5, navi, Zatchbot... Shout outs in no particular order go to Grizzly Adams, Kekule, Blackbolt, Ryzee119, Xbox7887, Incursion64, Redline99, Empreal96, Fredr1kh, Braxton, Natetronn... Keep your eye out for more future applications from Team Resurgent... Til then, enjoy, and we will see you on our discord................................................................................. ";
        private float charOffset = 0;
        private float scrollPos = 0;
        private float sin = 0;
        private Star[] stars = new Star[100];

        private Matrix4x4 viewMatrix;
        private Matrix4x4 projMatrix;
        private Matrix4x4 worldMatrix;

        private List<Vector3> objectPoints = new List<Vector3>();
        private List<int> objectIndices = new List<int>();

        Vector3 Vec3Mat4x4Multiply(Vector3 v, Matrix4x4 m)
        {
            float x = v.X * m.M11 + v.Y * m.M21 + v.Z * m.M31 + m.M41;
            float y = v.X * m.M12 + v.Y * m.M22 + v.Z * m.M32 + m.M42;
            float z = v.X * m.M13 + v.Y * m.M23 + v.Z * m.M33 + m.M43;
            float w = v.X * m.M14 + v.Y * m.M24 + v.Z * m.M34 + m.M44;
            return new Vector3(x, y, z) / w;
        }

        Vector3 Vec3Project(Vector3 v, Matrix4x4 world, Matrix4x4 view, Matrix4x4 proj)
        {
            Vector3 projectedPos = Vec3Mat4x4Multiply(v, worldMatrix * viewMatrix * projMatrix);
            return new Vector3(0 + (1 + projectedPos.X) * 600 / 2, 0 + (1 - projectedPos.Y) * 400 / 2, 0 + projectedPos.Z * 1);
        }


        public CreditsDialog()
        {
            var fontAtlas = ImGui.GetIO().Fonts;
            _font = fontAtlas.Fonts[1];

            var random = new Random();
            for (var i = 0; i < stars.Length; i++)
            {
                stars[i] = new Star
                {
                    Pos = new Vector2(random.Next(-300, 300), random.Next(-200, 200)),
                    Scale = (float)(random.NextDouble() * 2)
                };
            }

            viewMatrix = Matrix4x4.CreateLookAt(new Vector3(-8, 0, 0), Vector3.Zero, Vector3.UnitY);
            projMatrix = Matrix4x4.CreatePerspectiveFieldOfView((float)ConvertToRadians(60), 600.0f / 400.0f, 1, 10);
            worldMatrix = Matrix4x4.CreateRotationX((float)ConvertToRadians(0));

            objectPoints.Add(new Vector3(-2, -2, -2));
            objectPoints.Add(new Vector3(2, -2, -2));
            objectPoints.Add(new Vector3(2, 2, -2));
            objectPoints.Add(new Vector3(-2, 2, -2));
            objectPoints.Add(new Vector3(-2, -2, 2));
            objectPoints.Add(new Vector3(2, -2, 2));
            objectPoints.Add(new Vector3(2, 2, 2));
            objectPoints.Add(new Vector3(-2, 2, 2));

            objectIndices.Add(0);
            objectIndices.Add(1);
            objectIndices.Add(1);
            objectIndices.Add(2);
            objectIndices.Add(2);
            objectIndices.Add(3);
            objectIndices.Add(3);
            objectIndices.Add(0);

            objectIndices.Add(4);
            objectIndices.Add(5);
            objectIndices.Add(5);
            objectIndices.Add(6);
            objectIndices.Add(6);
            objectIndices.Add(7);
            objectIndices.Add(7);
            objectIndices.Add(4);

            objectIndices.Add(0);
            objectIndices.Add(4);
            objectIndices.Add(1);
            objectIndices.Add(5);
            objectIndices.Add(2);
            objectIndices.Add(6);
            objectIndices.Add(3);
            objectIndices.Add(7);
        }

        public void ShowModal(Config config)
        {
            _showModal = true;
            _config = config;
        }

        private void CloseModal()
        {
            Bass.StreamFree(m_playBackHandle);
            Bass.Free();

            _open = false;
            ImGui.CloseCurrentPopup();
        }

        public static double ConvertToRadians(double angle)
        {
            return Math.PI / 180 * angle;
        }

        private static void DrawLines(IReadOnlyList<Vector2> points, Vector2 location)
        {
            var lineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 0, 1));
            var drawList = ImGui.GetWindowDrawList();
            for (var i = 0; i < points.Count; i += 2)
            {
                var startLocation = location + points[i];
                var endLocation = location + points[i + 1];
                drawList.AddLine(startLocation, endLocation, lineColor);
            }
        }

        private void DrawStars(Vector2 location)
        {
            var sinPeak = (float)Math.Sin(ConvertToRadians(sin)) * 50;
            var cosPeak = (float)Math.Cos(ConvertToRadians(sin)) * 50;

            var random = new Random();
            var pointColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 1));
            var drawList = ImGui.GetWindowDrawList();
            for (var i = 0; i < stars.Length; i++)
            {
                var scaledPos = stars[i].Pos * stars[i].Scale;
                var startLocation = location + scaledPos + new Vector2(300 + sinPeak, 200 + cosPeak);
                var endLocation = location + scaledPos + new Vector2(301 + sinPeak, 201 + cosPeak);
                drawList.AddLine(startLocation, endLocation, pointColor);
                stars[i].Scale += 0.01f;
                
                if (stars[i].Scale >= 2.0f )
                {
                    stars[i].Pos = new Vector2(random.Next(-300, 300), random.Next(-200, 200));
                    stars[i].Scale = 0.01f;
                }
            }
        }

        private void DrawObject(Vector2 location)
        {
            var pointColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0, 1, 1));
            var lineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 1, 1));
            var drawList = ImGui.GetWindowDrawList();
            for (var i = 0; i < objectIndices.Count; i+=2)
            {
                var projected1 = Vec3Project(objectPoints[objectIndices[i]], worldMatrix, viewMatrix, projMatrix);
                var screenLocation1 = new Vector2(projected1.X, projected1.Y) + location;
                var projected2 = Vec3Project(objectPoints[objectIndices[i + 1]], worldMatrix, viewMatrix, projMatrix);
                var screenLocation2 = new Vector2(projected2.X, projected2.Y) + location;
                drawList.AddCircleFilled(screenLocation1, 3, pointColor);
                drawList.AddLine(screenLocation1, screenLocation2, lineColor);
            }
            worldMatrix = Matrix4x4.CreateRotationX((float)ConvertToRadians(sin)) *
                          Matrix4x4.CreateRotationY((float)ConvertToRadians(sin)) *
                          Matrix4x4.CreateRotationZ((float)ConvertToRadians(sin));
        }

        private void PlayModule()
        {
            Bass.Init();
            var musicData = ResourceLoader.GetEmbeddedResourceBytes("Stuart Wilson - Not Another Comic Bakery Remix.mp3", typeof(CreditsDialog).GetTypeInfo().Assembly);
            m_playBackHandle = Bass.CreateStream(musicData, 0, musicData.Length, BassFlags.Default | BassFlags.Loop);
            Bass.ChannelPlay(m_playBackHandle);

            m_signalProvider = new SignalProvider(DataFlags.FFT1024, true, true) { WindowType = WindowType.Hanning, };
            m_signalProvider.SetChannel(m_playBackHandle);
        }

        public bool Render()
        {
            if (_showModal)
            {
                _showModal = false;
                _open = true;

                PlayModule();

                ImGui.OpenPopup(Title);
            }

            if (!_open)
            {
                return false;
            }

            var open = true;

            if (!ImGui.BeginPopupModal(Title, ref open, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar))
            {
                CloseModal();
                return false;
            }

            var result = false;

            ImGui.PushFont(_font);

            DrawStars(ImGui.GetWindowPos());
            DrawObject(ImGui.GetWindowPos());  

            if (_config.LeechType > 0)
            {
                ImGui.SetCursorPos(new Vector2(10, 20));
                if (_config.LeechType == 1)
                {
                    ImGui.Text("Leech, Process & Delete Mode");
                }
                else if (_config.LeechType == 2)
                {
                    ImGui.Text("Leech, Process & Keep Mode");
                }
                else
                {
                    ImGui.Text("Leech, Keep Mode");
                }
            }

            if (m_signalProvider != null)
            {
                var channelData = m_signalProvider.DataSampleWindowed;
                if (channelData != null)
                {
                    var points = new List<Vector2>
                    {
                        new Vector2(0, 390)
                    };
                    var channelValues = channelData[0].AdjustToScale(0, 200, true, out _);
                    var channelDataLength = channelValues.Data.Length;
                    for (var dataIndex = 0; dataIndex < channelDataLength; dataIndex++)
                    {
                        var value = (int)channelValues.Data[dataIndex];
                        points.Add(new Vector2((dataIndex + 1) * (600.0f / (channelDataLength + 1)), 390 - value));
                        points.Add(new Vector2((dataIndex + 1) * (600.0f / (channelDataLength + 1)), 390 - value));
                    }
                    points.Add(new Vector2(600, 390));
                    DrawLines(points, ImGui.GetWindowPos());
                }
            }

            float x = 0;
            float y = 0;
            int i = (int)charOffset;
            while (x < 620)
            {

                var amplitude = 100.0f;
                var peak = (float)Math.Sin(ConvertToRadians(sin + y));
                var textSize = ImGui.CalcTextSize(Message[i].ToString());
                var textXOffset = (20 - textSize.X) / 2;
                ImGui.SetCursorPos(new Vector2(x - scrollPos + textXOffset, amplitude * 2 + peak * amplitude));
                ImGui.Text(Message[i].ToString());
                i++;
                if (i >= Message.Length)
                {
                    i = 0;
                }
                x += 20;

                sin += 0.05f;
                if (sin >= 360)
                {
                    sin -= 360;
                }
                y += 2f;
            }

            scrollPos++;
            if (scrollPos == 20)
            {
                scrollPos = 0;
                charOffset++;
                if (charOffset == Message.Length)
                {
                    charOffset = 0;
                }
            }

            ImGui.PopFont();

            if (ImGui.IsWindowAppearing())
            {
                ImGui.SetWindowSize(new Vector2(600, 400));
            }

            ImGui.SetCursorPos(new Vector2(0, 5));
            if (ImGui.InvisibleButton("##closeCredits", new Vector2(600, 390)))
            {                
                if (ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyAlt)
                {
                    _config.LeechType = (_config.LeechType + 1) % 4;
                    Config.SaveConfig(_config);
                }
                else
                {
                    result = true;
                    CloseModal();
                }
            }

            ImGui.EndPopup();

            return result;
        }
    }
}
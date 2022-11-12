using ImGuiNET;
using ManagedBass.FftSignalProvider;
using ManagedBass;
using Repackinator.Shared;
using System.Numerics;
using System.Reflection;
using System;

namespace RepackinatorUI
{
    public class CreditsDialog
    {
        private bool _showModal;
        private bool _open;

        ImFontPtr _font;

        private int m_playBackHandle;
        private SignalProvider? m_signalProvider;

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = "Repackinator brought to you by Team Resurgent and in collaboration with Team Cerbios, what application wouldn't be complete without some retro style credits... Repackinator is Open Source on our GitHub and is open to the Community to contribute to and help evolve the application into something amazing... Music is a remix of the C64 classic Comic Bakery by Stuart Wilson... Coding by EqUiNoX... Application Design by EqUiNoX, HoRnEyDvL, Hazeno... Testing by HoRnEyDvL, Hazeno, Rocky5... Shout outs in no particular order go to Grizzly Adams, Kekule, Blackbolt, Ryzee119, Xbox7887, Incursion64, Redline99, Empreal96, Fredr1kh, Braxton, Natetronn... Keep your eye out for more future applications from Team Resurgent... Til then, enjoy, and we will see you on our discord................................................................................. ";
        private float charOffset = 0;
        private float scrollPos = 0;
        private float sin = 0;
        private Vector2[] stars = new Vector2[100];
        private float[] starsSpeeds = new float[100];

        public CreditsDialog()
        {
            var fontAtlas = ImGui.GetIO().Fonts;
            _font = fontAtlas.Fonts[1];

            var random = new Random();
            for (var i = 0; i < stars.Length; i++)
            {
                stars[i] = new Vector2(random.Next(600), random.Next(400));
                starsSpeeds[i] = ((float)random.NextDouble() * 0.9f) + 0.1f;
            }
        }

        public void ShowModal()
        {
            _showModal = true;
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
            return (Math.PI / 180) * angle;
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
            var pointColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 1));
            var drawList = ImGui.GetWindowDrawList();
            for (var i = 0; i < stars.Length; i++)
            {
                var startLocation = location + stars[i];
                var endLocation = location + stars[i] + new Vector2(1, 1);
                drawList.AddLine(startLocation, endLocation, pointColor);
                stars[i].X = stars[i].X - starsSpeeds[i];
                if (stars[i].X < 0)
                {
                    stars[i].X = 600;
                }
            }
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
                ImGui.SetCursorPos(new Vector2((x - scrollPos) + textXOffset, (amplitude * 2) + (peak * amplitude)));
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
                result = true;
                CloseModal();
            }

            ImGui.EndPopup();

            return result;
        }
    }
}
using System;
using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Repackinator.Shared;
using SharpMik.Player;
using SharpMik.Drivers;
using SharpMik;
using System.Diagnostics;
using Veldrid.MetalBindings;
using System.Threading.Channels;
using System.Security.Cryptography.X509Certificates;

namespace RepackinatorUI
{
    public class CreditsDialog
    {
        private bool  _showModal;
        private bool _open;

        MikModule? _module;
        MikMod _player;
        ImFontPtr _font;

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = "Repackinator brought to you by Team Resurgent and in collaboration with Team Cerbios, what application wouldnt be complete without some retro style credits... Repackinator is Open Source on our GitHub and is open to the Community to contribute to and help evelove the application into something amazing... Music is from the awesome Amiga's S3M Tracker... Coding by EqUiNoX... Application Design by EqUiNoX, HoRnEyDvL, Hazeno... Testing by HoRnEyDvL, Hazeno, Rocky5... Shout outs go to Grizzly Adams, Kekule... Keep your eye out for more future applications from Team Resurgent... Til then, enjoy, and we will see on our discord.................................................................................. ";
        private float charOffset = 0;
        private float scrollPos = 0;
        private float sin = 0;
        private float[] chan_amplitudes = new float[64];
        private Vector2[] stars = new Vector2[100];
        private float[] starsSpeeds = new float[100];

        void PlayerStateChangeEvent(ModPlayer.PlayerState state)
        {
            if (_module == null)
            {
                return;
            }
            if (state == ModPlayer.PlayerState.kUpdated)
            {
                for (int i = 0; i < ModPlayer.NumberOfVoices(_module); i++)
                {
                    if (ModPlayer.NotePlayed[i])
                    {
                        chan_amplitudes[i] = 100;
                    }
                }
                return;
            }
            else if (state != ModPlayer.PlayerState.kStopped)
            {
                return;
            }
            PlayModule();
        }

        public CreditsDialog()
        {
            var fontAtlas = ImGui.GetIO().Fonts;
            _font = fontAtlas.Fonts[1];

            _player = new MikMod();
            _player.PlayerStateChangeEvent += new ModPlayer.PlayerStateChangedEvent(PlayerStateChangeEvent);

            var random = new Random();
            for (var i = 0; i <stars.Length; i++)
            {
                stars[i] = new Vector2(random.Next(600), random.Next(400));
                starsSpeeds[i] = ((float)random.NextDouble() * 0.9f) + 0.1f;
            }

            ModDriver.Mode = (ushort)(ModDriver.Mode | SharpMikCommon.DMODE_NOISEREDUCTION);
            try
            {
                _player.Init<NaudioDriver>("");
            }
            catch 
            {
                // do nothing
            }
        }

        public void ShowModal()
        {
            _showModal = true;
        }

        private void CloseModal()
        {
            ModPlayer.Player_Stop();
            ModDriver.MikMod_Exit();

            _open = false;
            ImGui.CloseCurrentPopup();
        }

        public double ConvertToRadians(double angle)
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
            var modStream = ResourceLoader.GetEmbeddedResourceStream("DINO.S3M", typeof(CreditsDialog).GetTypeInfo().Assembly);
            if (modStream != null)
            {
                _module = _player.Play(modStream);
            }
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

            if (_module != null)
            {
                var points = new List<Vector2>();
                points.Add(new Vector2(0, 390));
                for (var chan = 0; chan < ModPlayer.NumberOfVoices(_module); chan++)
                {
                    if (ModPlayer.NotePlayed[chan])
                    {
                        chan_amplitudes[chan] = 200;
                    }
                    else
                    {
                        chan_amplitudes[chan] = chan_amplitudes[chan] * 0.95f;
                    }

                    points.Add(new Vector2((chan + 1) * (600 / (ModPlayer.NumberOfVoices(_module) + 1)), 390 - chan_amplitudes[chan]));
                    points.Add(new Vector2((chan + 1) * (600 / (ModPlayer.NumberOfVoices(_module) + 1)), 390 - chan_amplitudes[chan]));
                }
                points.Add(new Vector2(600, 390));
                DrawLines(points, ImGui.GetWindowPos());
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
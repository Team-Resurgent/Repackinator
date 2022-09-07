using System;
using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Repackinator.Shared;
using SharpMik.Player;
using SharpMik.Drivers;
using SharpMik;
using System.Diagnostics;

namespace RepackinatorUI
{
    public class CreditsDialog
    {
        private bool  _showModal;
        private bool _open;

        MikModule? _module;
        MikMod _player;

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = "REPACKINATOR BROUGHT TO YOU BY TEAM RESURGENT AND IN COLLABORATION WITH TEAM CERBIOS, WHAT APPLICATION WOULDNT BE COMPLETE WITHOUT SOME RETRO STYLE CREDITS... REPACKINATOR IS OPEN SOURCE ON OUR GITHUB AND IS OPEN TO THE COMMUNITY TO CONTRIBUTE TO AND HELP EVELOVE THE APPLICATION INTO SOMETHING AMAZING... MUSIC IS FROM THE AWESOME AMIGA'S VISIONS MEGA DEMO II... CODING BY EQUINOX... APPLICATION DESIGN BY EQUINOX, HRNYDVL, HAZENO... TESTING BY HRNYDVL, HAZENO, ROCKY5... SHOUT OUTS GO TO GRIZZLY ADAMS, KEKULE... KEEP YOUR EYE OUT FOR MORE FUTURE APPLICATIONS FROM TEAM RESURGENT... TIL THEN, ENJOY, AND WE WILL SEE ON OUR DISCORD......................................... ";
        private float charOffset = 0;
        private float scrollPos = 0;
        private float sin = 0;

        void PlayerStateChangeEvent(ModPlayer.PlayerState state)
        {
            if (_module == null || state != ModPlayer.PlayerState.kStopped)
            {
                return;
            }
            _player.Play(_module);
        }


        public CreditsDialog()
        {
            _player = new MikMod();
            _player.PlayerStateChangeEvent += new ModPlayer.PlayerStateChangedEvent(PlayerStateChangeEvent);

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

        private void StartAudio()
        {
            var modStream = ResourceLoader.GetEmbeddedResourceStream("COMIC.MOD", typeof(CreditsDialog).GetTypeInfo().Assembly);
            if (modStream != null)
            {
                _module = _player.LoadModule(modStream);
                if (_module != null)
                {
                    _player.Play(_module);
                }
            }            

        }

        public bool Render()
        {
            if (_showModal)
            {
                _showModal = false;
                _open = true;

                var modStream = ResourceLoader.GetEmbeddedResourceStream("NEWGEN.MOD", typeof(CreditsDialog).GetTypeInfo().Assembly);
                if (modStream != null)
                {
                    _module = _player.LoadModule(modStream);
                    if (_module != null)
                    {
                        _player.Play(_module);
                    }
                }

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

            float x = 0;
            float y = 0;
            int i = (int)charOffset; 
            while (x < 610)
            {
                var aaa = (float)Math.Sin(ConvertToRadians(sin +y ));
                ImGui.SetCursorPos(new Vector2(x - scrollPos, 200 + (aaa * 100)));
                ImGui.Text(Message[i].ToString());
                i++;
                if (i >= Message.Length)
                {
                    i = 0;
                }
                x += 10;

                sin += 0.05f;
                if (sin >= 360) 
                {
                    sin -= 360;
                }
                y += 5;
            }

            scrollPos++;
            if (scrollPos == 10)
            {
                scrollPos = 0;
                charOffset++;
                if (charOffset == Message.Length)
                {
                    charOffset = 0;
                }
            }


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
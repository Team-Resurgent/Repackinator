using ImGuiNET;
using System.Numerics;

namespace Repackinator.UI
{
    public class SplashDialog
    {
        private bool m_show = false;
        private bool m_requestedClose = false;
        private int m_splashTexture;
        private DateTime m_started;

        public void ShowdDialog(int splashTexture)
        {
            m_started = DateTime.Now;
            m_splashTexture = splashTexture;
            m_show = true;
        }

        public bool Render()
        {
            if (m_show)
            {
                m_show = false;
                ImGui.OpenPopup("###splashDialog");
            }

            bool open = true;
            if (!ImGui.BeginPopupModal("###splashDialog", ref open, ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground))
            {
                return false;
            }

            var fade = 1.0f;
            var time = DateTime.Now - m_started;
            if (time.TotalMilliseconds > 1000)
            {
                fade = (float)Math.Max((1000 - (time.TotalMilliseconds - 3000)) / 1000, 0);
            }

            ImGui.Image(m_splashTexture, new Vector2(640, 480), Vector2.Zero, Vector2.One, new Vector4(1.0f, 1.0f, 1.0f, fade));

            if (fade == 0 && m_requestedClose == false)
            {
                m_requestedClose = true;
                ImGui.CloseCurrentPopup();
            }

            return false;
        }
    }
}

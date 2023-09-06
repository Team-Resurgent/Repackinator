using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Repackinator.UI;

namespace Repackinator
{
    public class Window : GameWindow
    {
        public Window() : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = new Vector2i(1600, 900), APIVersion = new Version(3, 3) })
        {
            var scaleFactor = System.Numerics.Vector2.One;
            if (TryGetCurrentMonitorScale(out var scaleX, out var scaleY))
            {
                scaleFactor = new System.Numerics.Vector2(scaleX, scaleY);
            }
            Controller = new ImGuiController(ClientSize.X, ClientSize.Y, scaleFactor);
        }

        public int Width => ClientSize.X;

        public int Height => ClientSize.Y;

        public Action? RenderUI { get; set; }

        public ImGuiController? Controller { get; set; }


        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            // Update the opengl viewport
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);

            // Tell ImGui of the new size
            Controller?.WindowResized(ClientSize.X, ClientSize.Y);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            Controller?.Update((float)e.Time, this);

            GL.ClearColor(new Color4(0, 0, 0, 255));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            RenderUI?.Invoke();

            Controller?.Render();

            ImGuiController.CheckGLError("End of frame");

            SwapBuffers();
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            Controller?.PressChar((char)e.Unicode);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            Controller?.MouseScroll(new System.Numerics.Vector2(e.Offset.X, e.Offset.Y));
        }
    }
}

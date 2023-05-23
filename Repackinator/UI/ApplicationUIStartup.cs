using System.Runtime.InteropServices;

namespace Repackinator.UI
{
    public static class ApplicationUIStartup
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_CLOSE = 0x0010;

        public static void Start(string version)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var handle = GetConsoleWindow();
                    SendMessage(handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                }

                var application = new ApplicationUI(version);
                application.Run();
            }
            catch (Exception ex)
            {
                var now = DateTime.Now.ToString("MMddyyyyHHmmss");
                File.WriteAllText($"Crashlog-{now}.txt", ex.ToString());
                System.Console.WriteLine($"Error: {ex}");
            }
        }
    }
}

using System.Runtime.InteropServices;

namespace Repackinator.UI
{
    public static class ApplicationUIStartup
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;

        public static void Start(string version)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var handle = GetConsoleWindow();
                    ShowWindow(handle, SW_HIDE);
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

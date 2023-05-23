using System.Runtime.InteropServices;

namespace Repackinator.UI
{
    public static class ApplicationUIStartup
    {
        public static void Start(string version)
        {
            try
            {
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

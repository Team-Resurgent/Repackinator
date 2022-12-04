using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
                //XisoUtility.CreateCCI(isoInput, @"E:\Dump", "TotalNewScrub", ".cci", true, null, default);

                //XisoUtility.ConvertCCItoISO(@"E:\Dump\TotalNewScrub.cci", @"E:\Dump\TotalNewScrubCCI.iso");

                //XisoUtility.CompareXISO(new XisoInput(new string[] { @"E:\Dump\TotalOrigScrubJoinmed.iso" }), new XisoInput(new string[] { @"E:\Dump\TotalNewScrubCCI.iso" }));

                //var result1 = new byte[0];
                //var isoInput1 = new XisoInput(new string[] { @"E:\Dump\Total Immersion Racing (USA).iso" });
                //XisoUtility.TryGetDefaultXbeFromXiso(isoInput1, ref result1);

                //var result2 = new byte[0];
                //var isoInput2 = new CciInput(new string[] { @"E:\Dump\TotalNewScrub.cci" });
                //XisoUtility.TryGetDefaultXbeFromXiso(isoInput2, ref result2);
                //File.WriteAllBytes(@"E:\Dump\Total.xbe", result2);


                //var b = new HashSet<uint>();
                //var s = new FileStream(@"G:\XboxOG\Total Immersion Racing (USA).iso", FileMode.Open);
                //var a = XisoUtility.GetDataSectorsFromXiso(s);
                //s.Dispose();

                //var q = new XisoInput(new string[] { @"G:\XboxOG\Total Immersion Racing (USA).iso" });    
                //var v = XisoUtility.GetDataSectorsFromXisoNew(q);
                //q.Dispose();

                //for (var i = 0; i < a.Count; i++)
                //{
                //    if (a.ElementAt(i) != v.ElementAt(i))
                //    {
                //        var gg = 1;
                //    }
                //}

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
            }
        }
    }
}

using RepackinatorUI;
using Resurgent.UtilityBelt.Library.Utilities;
using Resurgent.UtilityBelt.Library.Utilities.Xiso;
using SharpDX;

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

    var version = "v1.1.1";
    var application = new Application(version);
    application.Run();
}
catch (Exception ex)
{
    var now = DateTime.Now.ToString("MMddyyyyHHmmss");
    File.WriteAllText($"Crashlog-{now}.txt", ex.ToString());
}
using Repackinator;
using Mono.Options;

var shouldShowHelp = false;
var input = "";
var output = "";
var temp = "";
var grouping = "NONE";
var alternate = "NO";
var log = "";

var optionset = new OptionSet {
    { "i|input=", "Input folder", i => input = i },
    { "o|output=", "Output folder", o => output = o },
    { "g|grouping=", "Grouping (None *default*, Region, Letter, RegionLetter, LetterRegion)", g => grouping = g.ToUpper() },
    { "a|alt=", "Alternate Naming (No *default*, Yes)", a => alternate = a.ToUpper() },
    { "t|temp=", "Temp folder", t => temp = t },
    { "l|log=", "log file", l => log = l },
    { "h|help", "show this message and exit", h => shouldShowHelp = h != null },
};

try
{
    //"Title ID": "41430002",
    //    "Title Name": "All-Star Baseball '03",
    //    "Version": "01",
    //    "Region": "(USA)",
    //    "Archive Name": "All-Star Baseball 2003 featuring Derek Jeter (USA)",
    //    "XBE Title & Folder Name": "All-Star Baseball 2003 FT D.J (USA)",
    //    "XBE Title Length": "35",
    //    "ISO Name": "All-Star Baseball 2003 FT D.J (USA)",
    //    "ISO Name Length": "41",
    //    "Process": "Y"


    //var summarystring = new StringWriter();
    //var summaries = new List<XbeSummary>();
    //var files = Directory.GetFiles(@"G:\EasyFinancial\New Xbes");
    //foreach (var file in files)
    //{
    //    Console.WriteLine(file);
    //    var fileinput = File.ReadAllBytes(file);
    //    XbeUtility.TryGetXbeCert(fileinput, out var cert);
    //    var xbeSummary = cert.Value.ToXbeSummary(Path.GetFileNameWithoutExtension(file));
    //    summaries.Add(xbeSummary);
    //    summarystring.WriteLine(cert.Value.ToSummaryString(Path.GetFileNameWithoutExtension(file)));
    //}

    //File.WriteAllText(@"G:\EasyFinancial\New Xbes\XbeDataListMedia.txt", summarystring.ToString());

    //var attach = ResourceLoader.GetEmbeddedResourceBytes("attach.xbe");
    //var files = Directory.GetFiles(@"F:\XboxTool\Xbes");
    //foreach (var file in files)
    //{
    //    var game = File.ReadAllBytes(file);
    //    var name = Path.GetFileNameWithoutExtension(file);

    //    if (XbeUtility.TryGetXbeImage(game, ImageType.TitleImage, out var outputGameIcon)) {        
    //        if (XprUtility.ConvertXprToPng(outputGameIcon, out var outputGameImage)) {
    //            XbeUtility.TryReplaceXbeTitleImage(attach, outputGameImage);
    //        }
    //    }

    //    XbeUtility.ReplaceCertInfo(attach, game, "EqUiNoX", out var outputGame);
    //    if (outputGame != null)
    //    {
    //        File.WriteAllBytes($"F:\\XboxTool\\AttachXbes\\{name}-attach.xbe", outputGame);
    //    }
    //}

    //using var ms = new FileStream(@"G:\EasyFinancial\ESPN NHL 2K5 (USA)\NHL 2K6 (USA).iso", FileMode.Open);
    //using var os = new MemoryStream();
    //string error = "";
    //XisoUtility.TryExtractDefaultFromXiso(ms, os, ref error);
    //File.WriteAllBytes(@"G:\EasyFinancial\ESPN NHL 2K5 (USA)\NHL 2K6 (USA).xbe", os.ToArray());

    //var outputPath = $"F:\\XboxTool\\XbeImages";

    //var files = Directory.GetFiles(@"H:\Xbes");
    //foreach (var file in files)
    //{
    //    Console.WriteLine(file);
    //    var name = Path.GetFileNameWithoutExtension(file);
    //    var fileinput = File.ReadAllBytes(file);

    //    var logoUtility = new LogoUtility();
    //    XbeUtility.TryGetXbeImage(fileinput, ImageType.LogoImage, out var fileoutput1);
    //    logoUtility.DecodeLogoImage(fileoutput1, out var imageoutput1);
    //    if (imageoutput1 != null)
    //    {
    //        //File.WriteAllBytes(Path.Combine(outputPath, $"{name}-banner.png"), imageoutput1);
    //    }

    //    if (XbeUtility.TryGetXbeImage(fileinput, ImageType.TitleImage, out var fileoutput2))
    //    {
    //        if (XprUtility.ConvertXprToPng(fileoutput2, out var imageoutput2))
    //        {
    //            File.WriteAllBytes(Path.Combine(outputPath, $"{name}-title.png"), imageoutput2);
    //        }
    //        else
    //        {
    //            Console.WriteLine("Error");
    //        }
    //    }

    //    if (XbeUtility.TryGetXbeImage(fileinput, ImageType.SaveImage, out var fileoutput3))
    //    {
    //        if (XprUtility.ConvertXprToPng(fileoutput3, out var imageoutput3))
    //        {
    //            File.WriteAllBytes(Path.Combine(outputPath, $"{name}-save.png"), imageoutput3);
    //        }
    //        else
    //        {
    //            Console.WriteLine("Error");
    //        }
    //    }
    //}

    optionset.Parse(args);
    if (shouldShowHelp || args.Length == 0)
    {
        Console.WriteLine("Usage: Repackinator");
        Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
        Console.WriteLine("Credits go to HoRnEyDvL, Hazeno, Rocky5, Team Cerbios.");
        Console.WriteLine();
        Console.WriteLine("Usage: Repackinator [options]+");
        Console.WriteLine();
        optionset.WriteOptionDescriptions(Console.Out);
        return;
    }

    if (string.IsNullOrEmpty(input))
    {
        throw new OptionException("input not specified.", "input");
    }

    input = Path.GetFullPath(input);
    if (!Directory.Exists(input))
    {
        throw new OptionException("input is not a valid directory.", "input");
    }

    if (string.IsNullOrEmpty(output))
    {
        throw new OptionException("output not specified.", "output");
    }

    if (!string.Equals(grouping, "NONE") && !string.Equals(grouping, "REGION") && !string.Equals(grouping, "LETTER") && !string.Equals(grouping, "REGIONLETTER") && !string.Equals(grouping, "LETTERREGION"))
    {
        throw new OptionException("grouping is not valid.", "grouping");
    }

    if (!string.Equals(alternate, "NO") && !string.Equals(alternate, "YES"))
    {
        throw new OptionException("alternate is not valid.", "alternate");
    }

    output = Path.GetFullPath(output);
    if (!Directory.Exists(output))
    {
        Directory.CreateDirectory(output);
    }

    if (string.IsNullOrEmpty(temp))
    {
        temp = Path.Combine(Path.GetTempPath(), "Repackinator");
    }

    temp = Path.GetFullPath(temp);
    if (!Directory.Exists(temp))
    {
        Directory.CreateDirectory(temp);
    }

    if (!string.IsNullOrEmpty(log))
    {
        log = Path.GetFullPath(log);
    }
     
    Repacker.StartConversion(input, output, grouping, alternate, temp, log);

    Console.WriteLine("Done!");
    Console.ReadLine();
}
catch (OptionException e)
{
    Console.Write("Repackinator by EqUiNoX: ");
    Console.WriteLine(e.Message);
    Console.WriteLine("Try `Repackinator --help' for more information.");
}

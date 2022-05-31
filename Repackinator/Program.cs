using QuikIso;
using Mono.Options;

var shouldShowHelp = false;
var input = "";
var output = "";
var temp = "";
var log = "";

var optionset = new OptionSet {
    { "i|input=", "Input folder", i => input = i },
    { "o|output=", "Output folder", o => output = o },
    { "t|temp=", "Temp folder", t => temp = t },
    { "l|log=", "log file", l => log = l },
    { "h|help", "show this message and exit", h => shouldShowHelp = h != null },
};

try
{
    //var files = Directory.GetFiles(@"F:\XboxTool\Isos");
    //foreach (var file in files)
    //{
    //    var extension = Path.GetExtension(file).ToLower();

    //}


    //var attach = ResourceLoader.GetEmbeddedResourceBytes("attach.xbe");
    //var files = Directory.GetFiles(@"F:\XboxTool\Xbes");
    //foreach (var file in files)
    //{
    //    var xbe = new XbeUtility();
    //    var game = File.ReadAllBytes(file);
    //    xbe.ReplaceCertInfo(attach, game, out var outputGame);
    //    var name = Path.GetFileNameWithoutExtension(file);
    //    if (outputGame != null)
    //    {
    //        File.WriteAllBytes($"F:\\XboxTool\\AttachXbes\\{name}-attach.xbe", outputGame);
    //    }
    //}


    //var outputPath = $"F:\\XboxTool\\XbeImagesFix";
    //var a = new XbeUtility();
    //var files = Directory.GetFiles(@"F:\XboxTool\XbesFix");
    //foreach (var file in files)
    //{
    //    Console.WriteLine(file);
    //    var name = Path.GetFileNameWithoutExtension(file);
    //    var fileinput = File.ReadAllBytes(file);

    //    var logoUtility = new LogoUtility();
    //    a.TryGetXbeImage(fileinput, ImageType.LogoImage, out var fileoutput1);
    //    logoUtility.DecodeLogoImage(fileoutput1, out var imageoutput1);
    //    if (imageoutput1 != null)
    //    {
    //        File.WriteAllBytes(Path.Combine(outputPath, $"{name}-banner.png"), imageoutput1);
    //    }

    //    var xprUtility = new XprUtility();
    //    if (a.TryGetXbeImage(fileinput, ImageType.TitleImage, out var fileoutput2))
    //    {
    //        if (xprUtility.ConvertXprToPng(fileoutput2, out var imageoutput2))
    //        {
    //            File.WriteAllBytes(Path.Combine(outputPath, $"{name}-title.png"), imageoutput2);
    //        }
    //        else
    //        {
    //            Console.WriteLine("");
    //        }
    //    }

    //    if (a.TryGetXbeImage(fileinput, ImageType.SaveImage, out var fileoutput3))
    //    {
    //        if (xprUtility.ConvertXprToPng(fileoutput3, out var imageoutput3))
    //        {
    //            File.WriteAllBytes(Path.Combine(outputPath, $"{name}-save.png"), imageoutput3);
    //        }
    //        else
    //        {
    //            Console.WriteLine("");
    //        }
    //    }
    //}





    optionset.Parse(args);
    if (shouldShowHelp || args.Length == 0)
    {
        Console.WriteLine("Usage: Repackinator");
        Console.WriteLine("Repackinator by EqUiNoX original xbox utility.");
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
     
    Repacker.StartConversion(input, output, temp, log);

    Console.WriteLine("Done!");
    Console.ReadLine();
}
catch (OptionException e)
{
    Console.Write("Repackinator by EqUiNoX: ");
    Console.WriteLine(e.Message);
    Console.WriteLine("Try `Repackinator --help' for more information.");
}

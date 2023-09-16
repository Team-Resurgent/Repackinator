using System.Globalization;
using Repackinator.Shell;
using Repackinator.Console;
using Repackinator.UI;

var version = "v1.4.4";

ContextMenu.RegisterContext();
CultureInfo.DefaultThreadCurrentUICulture = Thread.CurrentThread.CurrentCulture;
if (args.Length > 0)
{
    ConsoleStartup.Process(version, args);
}
else
{
    ApplicationUIStartup.Start(version);    
}

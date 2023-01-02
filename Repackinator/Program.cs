using Repackinator.Shell;
using Repackinator.Console;
using Repackinator.UI;

var version = "v1.2.4";

if (OperatingSystem.IsWindows())
{
    if (args.Length == 1 && args[0].Equals("Unregister", StringComparison.CurrentCultureIgnoreCase))
    {        
        var result = ContextMenu.UnregisterContext();
        if (result)
        {
            Console.WriteLine("Context menu removed.");
        }
        else
        {
            Console.WriteLine("Failed to remove context menu (need to run as admin).");
        }
        return;
    }
}

ContextMenu.RegisterContext();

if (args.Length > 0)
{
    ConsoleStartup.Start(version, args);
}
else
{
    ApplicationUIStartup.Start(version);    
}

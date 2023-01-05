using Repackinator.Shell;
using Repackinator.Console;
using Repackinator.UI;

var version = "v1.2.7";

if (args.Length > 0)
{
    ConsoleStartup.Process(version, args);
}
else
{
    ApplicationUIStartup.Start(version);    
}

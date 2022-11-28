using Repackinator;

var version = "v1.1.1";

ShellExtension.RegisterContext();

if (args.Length > 0)
{
    ConsoleStartup.Start(version, args);
}
else
{
    ApplicationUIStartup.Start(version);    
}

Console.ReadLine();
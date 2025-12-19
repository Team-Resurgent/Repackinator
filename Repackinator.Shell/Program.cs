using Repackinator.Shell.Console;

namespace Repackinator.Shell
{
    internal sealed class Program
    {
        public static void Main(string[] args)
        {
            ConsoleStartup.Process(Repackinator.Core.Version.Value, args);
        }
    }
}


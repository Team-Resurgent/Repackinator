using Mono.Options;

namespace Repackinator.Console
{
    public static class ConsoleUtil
    {
        public static void ShowHelpHeader(string version)
        {
            System.Console.WriteLine($"Repackinator {version}");
            System.Console.WriteLine("Repackinator by EqUiNoX, original xbox utility.");
            System.Console.WriteLine("Credits go to HoRnEyDvL, Hazeno, Rocky5, navi, Fredr1kh, Natetronn, Incursion64, Zatchbot, Team Cerbios.");
            System.Console.WriteLine();
            System.Console.WriteLine("Usage: Repackinator [options]+");
            System.Console.WriteLine();
        }

        public static void ShowOptionException(OptionException optionException)
        {
            System.Console.Write("Repackinator by EqUiNoX: ");
            System.Console.WriteLine(optionException.Message);
            System.Console.WriteLine("Try `Repackinator --help' for more information.");
        }

        public static void ProcessWait(bool wait)
        {
            if (!wait)
            {
                return;
            }
            System.Console.WriteLine();
            System.Console.Write("Press any key to continue.");
            System.Console.ReadKey();
        }
    }
}

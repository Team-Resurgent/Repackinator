using Avalonia;
using Avalonia.OpenGL;
using Avalonia.ReactiveUI;
using Repackinator.Core.Console;
using Repackinator.Core.Shell;
using System;
using System.Globalization;
using System.Threading;

namespace Repackinator
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            ContextMenu.RegisterContext();
            if (args.Length > 0)
            {
                ConsoleStartup.Process(Repackinator.Core.Version.Value, args);
            }
            else
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();
    }
}

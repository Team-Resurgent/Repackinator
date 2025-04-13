using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using Repackinator.Utils;
using Repackinator.ViewModels;
using System;
using System.Threading.Tasks;

namespace Repackinator.Views
{

    public partial class MainWindow : Window
    {
        private void GameDataListCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.GameDataListCellEditEnding(sender, e);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            WindowLocator.MainWindow = this;
            //this.AttachDevTools(new KeyGesture(Key.F12, KeyModifiers.Control | KeyModifiers.Alt));

            Title = $"Repackinator V{Repackinator.Core.Version.Value}";

            Opened += async (_, _) =>
            {
                await Task.Delay(2000);

                var animationFadeIn = Resources["FadeInAnimation"] as Animation;
                var animationFadeOut = Resources["FadeOutAnimation"] as Animation;
                if (animationFadeIn != null && animationFadeOut != null)
                {
                    await Task.WhenAll(
                        animationFadeIn.RunAsync(MainContent),
                        animationFadeOut.RunAsync(SplashView)
                   );
                }
                SplashView.IsVisible = false;
            };
        }
    }
}
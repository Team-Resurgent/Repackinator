using Avalonia.Controls;
using Repackinator.Core.Models;
using Repackinator.ViewModels;

namespace Repackinator;

public partial class AttachUpdateWindow : Window
{
    public AttachUpdateWindow()
    {
        InitializeComponent();

        DataContext = new AttachUpdateViewModel(this, [], new Config());
    }

    public AttachUpdateWindow(GameData[] gameDataArray, Config config)
    {
        InitializeComponent();

        var logViewModel = new AttachUpdateViewModel(this, gameDataArray, config);
        DataContext = logViewModel;

        Opened += async (_, _) =>
        {
            await logViewModel.StartAsync();
        };
    }
}
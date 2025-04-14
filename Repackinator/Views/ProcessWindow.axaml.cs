using Avalonia.Controls;
using Repackinator.Core.Models;
using Repackinator.ViewModels;

namespace Repackinator;

public partial class ProcessWindow : Window
{
    public GameData[]? GameDataList;

    public ProcessWindow()
    {
        InitializeComponent();

        DataContext = new AttachUpdateViewModel(this, [], new Config());
    }

    public ProcessWindow(GameData[] gameDataArray, Config config)
    {
        InitializeComponent();

        var logViewModel = new ProcessViewModel(this, gameDataArray, config);
        DataContext = logViewModel;

        Opened += async (_, _) =>
        {
            GameDataList = await logViewModel.StartAsync();
        };
    }
}
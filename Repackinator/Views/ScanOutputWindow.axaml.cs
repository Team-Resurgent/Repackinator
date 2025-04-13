using Avalonia.Controls;
using Repackinator.Core.Models;
using Repackinator.ViewModels;

namespace Repackinator;

public partial class ScanOutputWindow : Window
{
    public GameData[]? GameDataList;

    public ScanOutputWindow()
    {
        InitializeComponent();

        DataContext = new AttachUpdateViewModel(this, [], new Config());
    }

    public ScanOutputWindow(GameData[] gameDataArray, Config config)
    {
        InitializeComponent();

        var logViewModel = new ScanOutputViewModel(this, gameDataArray, config);
        DataContext = logViewModel;

        Opened += (_, _) =>
        {
            GameDataList = logViewModel.Start();
        };
    }
}
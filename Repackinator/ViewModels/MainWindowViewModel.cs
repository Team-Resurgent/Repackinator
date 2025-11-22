using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using Repackinator.Core.Models;
using System.Linq;
using Repackinator.Models;
using Avalonia.Controls;
using System.Reactive;
using ReactiveUI;
using DynamicData;
using Avalonia.Data;
using Avalonia.Platform.Storage;
using Repackinator.Utils;
using System.Reactive.Linq;
using System.Windows.Input;
using Repackinator.Views;
using Mono.Options;
using System.Reflection.Metadata;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using Repackinator.Core.Helpers;
using DynamicData.Kernel;
using Avalonia.Controls.Shapes;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Repackinator.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private const string mAllSections = "All Sections";

        private Config mLoadedConfig = new Config();
        private GameData[] mLoadedGameDataList = [];

        public ICommand CloseCommand { get; }
        public ICommand ProcessOptionCommand { get; }
        public ICommand SelectPathCommand { get; }
        public ICommand ShowAboutCommand { get; }
        public ICommand SaveChangesCommand { get; }
        public ICommand ExportSelectedCommand { get; }
        public ICommand ScanOutputCommand { get; }
        public ICommand AttachUpdateCommand { get; }
        public ICommand ProcessCommand { get; }

        public ObservableCollection<GameDataFilter> GameDataFilterList { get; set; }

        private ObservableCollection<GameData> mFilteredGameDataList;
        public ObservableCollection<GameData> FilteredGameDataList
        {
            get => mFilteredGameDataList;
            set => this.RaiseAndSetIfChanged(ref mFilteredGameDataList, value);
        }

        #region Search Properties

        public ObservableCollection<GameDataSection> GameDataSectionList { get; set; }

        private GameDataSection mSelectedGameDataSection;
        public GameDataSection SelectedGameDataSection
        {
            get => mSelectedGameDataSection;
            set => this.RaiseAndSetIfChanged(ref mSelectedGameDataSection, value);
        }

        private GameDataFilter mSelectedGameDataFilter;
        public GameDataFilter SelectedGameDataFilter
        {
            get => mSelectedGameDataFilter;
            set => this.RaiseAndSetIfChanged(ref mSelectedGameDataFilter, value);
        }

        private string mSearchForFilter = string.Empty;
        public string SearchForFilter
        {
            get => mSearchForFilter;
            set => this.RaiseAndSetIfChanged(ref mSearchForFilter, value);
        }

        private bool mShowInvalid = false;
        public bool ShowInvalid
        {
            get => mShowInvalid;
            set => this.RaiseAndSetIfChanged(ref mShowInvalid, value);
        }

        private void OnSearchChanged()
        {
            var result = new List<GameData>();
            for (int i = 0; i < mLoadedGameDataList.Length; i++)
            {
                if (IsGameDataFiltered(mLoadedGameDataList[i]))
                {
                    continue;
                }
                result.Add(mLoadedGameDataList[i]);
            }
            FilteredGameDataList = new ObservableCollection<GameData>(result);
        }

        #endregion

        #region Config Properties

        public ObservableCollection<GroupingOption> GroupingOptionList { get; set; }

        private GroupingOption mSelectedGroupingOption;
        public GroupingOption SelectedGroupingOption
        {
            get => mSelectedGroupingOption;
            set => this.RaiseAndSetIfChanged(ref mSelectedGroupingOption, value);
        }

        private bool mUseUppercase = false;
        public bool UseUppercase
        {
            get => mUseUppercase;
            set => this.RaiseAndSetIfChanged(ref mUseUppercase, value);
        }

        public ObservableCollection<CompressOption> CompressOptionList { get; set; }

        private CompressOption mSelectedCompressOption;
        public CompressOption SelectedCompressOption
        {
            get => mSelectedCompressOption;
            set => this.RaiseAndSetIfChanged(ref mSelectedCompressOption, value);
        }
        public ObservableCollection<ScrubOption> ScrubOptionList { get; set; }

        private ScrubOption mSelectedScrubOption;
        public ScrubOption SelectedScrubOption
        {
            get => mSelectedScrubOption;
            set => this.RaiseAndSetIfChanged(ref mSelectedScrubOption, value);
        }

        private bool mTraverseInputSubdirs = false;
        public bool TraverseInputSubdirs
        {
            get => mTraverseInputSubdirs;
            set => this.RaiseAndSetIfChanged(ref mTraverseInputSubdirs, value);
        }

        private bool mDoNotSplitISO = false;
        public bool DoNotSplitISO
        {
            get => mDoNotSplitISO;
            set => this.RaiseAndSetIfChanged(ref mDoNotSplitISO, value);
        }

        private string mInputFolder = string.Empty;
        public string InputFolder
        {
            get => mInputFolder;
            set => this.RaiseAndSetIfChanged(ref mInputFolder, value);
        }

        private string mOutputFolder = string.Empty;
        public string OutputFolder
        {
            get => mOutputFolder;
            set => this.RaiseAndSetIfChanged(ref mOutputFolder, value);
        }

        private string mUnpackFolder = string.Empty;
        public string UnpackFolder
        {
            get => mUnpackFolder;
            set => this.RaiseAndSetIfChanged(ref mUnpackFolder, value);
        }

        private void OnConfigChanged()
        {
            mLoadedConfig.Section = mSelectedGameDataSection.Name;
            mLoadedConfig.FilterType = mSelectedGameDataFilter.Type;
            mLoadedConfig.GroupingOption = mSelectedGroupingOption.Type;
            mLoadedConfig.Uppercase = mUseUppercase;
            mLoadedConfig.CompressOption = mSelectedCompressOption.Type;
            mLoadedConfig.ScrubOption = mSelectedScrubOption.Type;
            mLoadedConfig.RecurseInput = mTraverseInputSubdirs;
            mLoadedConfig.NoSplit = mDoNotSplitISO;
            mLoadedConfig.InputPath = mInputFolder;
            mLoadedConfig.OutputPath = mOutputFolder;
            mLoadedConfig.UnpackPath = mUnpackFolder;
            Config.SaveConfig(mLoadedConfig);
        }

        #endregion

        private bool IsGameDataFiltered(GameData gameData)
        {
            if (!mSelectedGameDataSection.Name.Equals(mAllSections, StringComparison.CurrentCultureIgnoreCase) && !gameData.Section.Equals(mSelectedGameDataSection.Name, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            if (mShowInvalid && gameData.IsValid)
            {
                return true;
            }

            if (string.IsNullOrEmpty(mSearchForFilter))
            {
                return false;
            }

            if (mSelectedGameDataFilter.Type == GameDataFilterType.Process)
            {
                return !gameData.Process.Contains(mSearchForFilter, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (mSelectedGameDataFilter.Type == GameDataFilterType.TitleID)
            {
                return !gameData.TitleID.Contains(mSearchForFilter, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (mSelectedGameDataFilter.Type == GameDataFilterType.Region)
            {
                return !gameData.Region.Contains(mSearchForFilter, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (mSelectedGameDataFilter.Type == GameDataFilterType.Version)
            {
                return !gameData.Version.Contains(mSearchForFilter, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (mSelectedGameDataFilter.Type == GameDataFilterType.TitleName)
            {
                return !gameData.TitleName.Contains(mSearchForFilter, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (mSelectedGameDataFilter.Type == GameDataFilterType.Letter)
            {
                return !gameData.Letter.Contains(mSearchForFilter, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (mSelectedGameDataFilter.Type == GameDataFilterType.XBETitle)
            {
                return !gameData.XBETitle.Contains(mSearchForFilter, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (mSelectedGameDataFilter.Type == GameDataFilterType.FolderName)
            {
                return !gameData.FolderName.Contains(mSearchForFilter, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (mSelectedGameDataFilter.Type == GameDataFilterType.ISOName)
            {
                return !gameData.ISOName.Contains(mSearchForFilter, StringComparison.CurrentCultureIgnoreCase);
            }
            else if (mSelectedGameDataFilter.Type == GameDataFilterType.ISOChecksum)
            {
                return !gameData.ISOChecksum.Contains(mSearchForFilter, StringComparison.CurrentCultureIgnoreCase);
            }
            return false;
        }

        private async Task SelectPath(string arg)
        {
            if (WindowLocator.MainWindow == null)
            {
                return;
            }
            var options = new FolderPickerOpenOptions
            {
                Title = $"Select ${arg} Folder",
                AllowMultiple = false
            };
              
            var result = await WindowLocator.MainWindow.StorageProvider.OpenFolderPickerAsync(options);
            if (result.Count > 0)
            {
                var path = result[0].Path.LocalPath;
                if (arg == "Input")
                {
                    InputFolder = path;
                }
                else if (arg == "Output")
                {
                    OutputFolder = path;
                }
                else if (arg == "Unpack")
                {
                    UnpackFolder = path;
                }
            }
        }

        public void GameDataListCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.DataContext is GameData gameData)
            {
                var index = Array.FindIndex(mLoadedGameDataList, x => x.Index == gameData.Index);
                if (index != -1)
                {
                    mLoadedGameDataList[index] = gameData;
                }
            }
        }

        public MainWindowViewModel()
        {
            CloseCommand = ReactiveCommand.Create<string>((s) =>
            {
                if (WindowLocator.MainWindow == null)
                {
                    return;
                }
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    WindowLocator.MainWindow.Close();
                });
            });

            ProcessOptionCommand = ReactiveCommand.Create<string>((s) =>
            {
                if (mFilteredGameDataList == null)
                {
                    return;
                }
                for (int i = 0; i < mFilteredGameDataList.Count; i++) 
                {
                    GameData gameDataSource = mFilteredGameDataList[i];
                    var destIndex = Array.FindIndex(mLoadedGameDataList, x => x.Index == gameDataSource.Index);
                    if (s.Equals("Enable"))
                    {
                        mLoadedGameDataList[destIndex].Process = "Y";
                    }
                    else if (s.Equals("Disable"))
                    {
                        mLoadedGameDataList[destIndex].Process = "N";
                    }
                    else if (s.Equals("Invert"))
                    {
                        mLoadedGameDataList[destIndex].Process = mLoadedGameDataList[destIndex].Process == "Y" ? "N" : "Y";
                    }
                }
                OnSearchChanged();
            });

            SelectPathCommand = ReactiveCommand.Create<string>(async (s) => 
            {
                await SelectPath(s);
            });

            ShowAboutCommand = ReactiveCommand.Create(() =>
            {
                if (WindowLocator.MainWindow == null)
                {
                    return;
                }
                var about = new AboutWindow();
                about.ShowDialog(WindowLocator.MainWindow);
    
            });

            SaveChangesCommand = ReactiveCommand.Create(() =>
            {
                if (WindowLocator.MainWindow == null)
                {
                    return;
                }
                GameDataHelper.SaveGameData(mLoadedGameDataList);

                var messageWindow = new MessageWindow("Save Changes", "Game data list has been saved.");
                messageWindow.ShowDialog(WindowLocator.MainWindow);
            });

            ExportSelectedCommand = ReactiveCommand.Create(async () =>
            {
                if (WindowLocator.MainWindow == null || WindowLocator.MainWindow is not MainWindow mainWindow)
                {
                    return;
                }

                var selectedItems = mainWindow.GameDataGrid.SelectedItems;
                if (selectedItems.Count == 0)
                {
                    return;
                }

                var applicationPath = Utility.GetApplicationPath();
                if (applicationPath == null)
                {
                    return;
                }

                var options = new FilePickerSaveOptions
                {
                    Title = "Save Selected",
                    DefaultExtension = ".txt",
                    SuggestedFileName = "Repackinator-Export.txt",
                    FileTypeChoices = [FilePickerFileTypes.TextPlain]
                };
                
                var result = await WindowLocator.MainWindow.StorageProvider.SaveFilePickerAsync(options);
                if (result == null)
                {
                    return;
                }

                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(
                    "Title ID".PadRight(8) + " : " +
                    "Title Name".PadRight(40) + " : " +
                    "Version".PadRight(8) + " : " +
                    "Region".PadRight(30) + " : " +
                    "Letter".PadRight(6) + " : " +
                    "XBE Title".PadRight(40) + " : " +
                    "Folder Name".PadRight(42) + " : " +
                    "ISO Name".PadRight(36) + " : " +
                    "ISO Checksum".PadRight(8));

                for (int i = 0; i < selectedItems.Count; i++)
                {
                    if (selectedItems[i] is not GameData gameData)
                    {
                        continue;
                    }
                    stringBuilder.AppendLine($"{gameData.TitleID.PadRight(8)} : {gameData.TitleName.PadRight(40)} : {gameData.Version.PadRight(8)} : {gameData.Region.PadRight(30)} : {gameData.Letter.PadRight(6)} : {gameData.XBETitle.PadRight(40)} : {gameData.FolderName.PadRight(42)} : {gameData.ISOName.PadRight(36)} : {gameData.ISOChecksum.PadRight(8)}");
                }

                File.WriteAllText(result.Path.LocalPath, stringBuilder.ToString());

                var messageWindow = new MessageWindow("Export Selected", "Game data selection has been saved.");
                await messageWindow.ShowDialog(WindowLocator.MainWindow);

            });

            ScanOutputCommand = ReactiveCommand.Create(() =>
            {
                if (WindowLocator.MainWindow == null || mSelectedGameDataSection == null)
                {
                    return;
                }

                var gamesToProcess = mLoadedGameDataList;
                if (!mSelectedGameDataSection.Name.Equals(mAllSections, StringComparison.CurrentCultureIgnoreCase))
                {
                    gamesToProcess = mLoadedGameDataList.Where(s => s.Section.Equals(mSelectedGameDataSection.Name)).ToArray();
                }

                var scanOutputWindow = new ScanOutputWindow(gamesToProcess, mLoadedConfig);
                scanOutputWindow.ShowDialog(WindowLocator.MainWindow);
                scanOutputWindow.Closing += (s, e) =>
                {
                    if (scanOutputWindow.GameDataList == null)
                    {
                        return;
                    }
                    for (int i = 0; i < scanOutputWindow.GameDataList.Length; i++)
                    {
                        GameData gameDataSource = scanOutputWindow.GameDataList[i];
                        var destIndex = Array.FindIndex(mLoadedGameDataList, x => x.Index == gameDataSource.Index);
                        mLoadedGameDataList[destIndex].Process = gameDataSource.Process;
                    }
                    OnSearchChanged();
                };
            });

            AttachUpdateCommand = ReactiveCommand.Create(() =>
            {
                if (WindowLocator.MainWindow == null || mSelectedGameDataSection == null)
                {
                    return;
                }

                var gamesToProcess = mLoadedGameDataList;
                if (!mSelectedGameDataSection.Name.Equals(mAllSections, StringComparison.CurrentCultureIgnoreCase))
                {
                    gamesToProcess = mLoadedGameDataList.Where(s => s.Section.Equals(mSelectedGameDataSection.Name)).ToArray();
                }

                var attachUpdateWindow = new AttachUpdateWindow(gamesToProcess, mLoadedConfig);
                attachUpdateWindow.ShowDialog(WindowLocator.MainWindow);
            });

            ProcessCommand = ReactiveCommand.Create(() =>
            {
                if (WindowLocator.MainWindow == null || mSelectedGameDataSection == null)
                {
                    return;
                }

                var gamesToProcess = mLoadedGameDataList;
                if (!mSelectedGameDataSection.Name.Equals(mAllSections, StringComparison.CurrentCultureIgnoreCase))
                {
                    gamesToProcess = mLoadedGameDataList.Where(s => s.Section.Equals(mSelectedGameDataSection.Name)).ToArray();
                }

                var processWindow = new ProcessWindow(gamesToProcess, mLoadedConfig);
                processWindow.ShowDialog(WindowLocator.MainWindow);
                processWindow.Closing += (s, e) =>
                {
                    if (processWindow.GameDataList == null)
                    {
                        return;
                    }
                    for (int i = 0; i < processWindow.GameDataList.Length; i++)
                    {
                        GameData gameDataSource = processWindow.GameDataList[i];
                        var destIndex = Array.FindIndex(mLoadedGameDataList, x => x.Index == gameDataSource.Index);
                        mLoadedGameDataList[destIndex].Process = gameDataSource.Process;
                    }
                    OnSearchChanged();
                };
            });

            mLoadedConfig = Config.LoadConfig();

            mLoadedGameDataList = GameDataHelper.LoadGameData() ?? [];

            var allSection = new GameDataSection(mAllSections);
            var gameDataSections = mLoadedGameDataList.Select(s => new GameDataSection(s.Section)).DistinctBy(s => s.Name).OrderBy(s => s.Name).Prepend(allSection).ToList();
            GameDataSectionList = new ObservableCollection<GameDataSection>(gameDataSections);
            var defaultSection = gameDataSections.Where(s => s.Name.Equals(mLoadedConfig.Section, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault() ?? allSection;
            mSelectedGameDataSection = defaultSection;

            var gameDataFilters = Enum.GetValues(typeof(GameDataFilterType)).Cast<GameDataFilterType>().Select(e => new GameDataFilter(e)).ToList();
            GameDataFilterList = new ObservableCollection<GameDataFilter>(gameDataFilters);
            var defaultFilter = gameDataFilters.Where(s => s.Type == mLoadedConfig.FilterType).FirstOrDefault() ?? new GameDataFilter(GameDataFilterType.TitleName);
            mSelectedGameDataFilter = defaultFilter;

            var groupingOptions = Enum.GetValues(typeof(GroupingOptionType)).Cast<GroupingOptionType>().Select(e => new GroupingOption(e)).ToList();
            GroupingOptionList = new ObservableCollection<GroupingOption>(groupingOptions);
            var defaultGroupingOption = groupingOptions.Where(s => s.Type == mLoadedConfig.GroupingOption).FirstOrDefault() ?? new GroupingOption(GroupingOptionType.None);
            mSelectedGroupingOption = defaultGroupingOption;

            mUseUppercase = mLoadedConfig.Uppercase;

            var compressOptions = Enum.GetValues(typeof(CompressOptionType)).Cast<CompressOptionType>().Select(e => new CompressOption(e)).ToList();
            CompressOptionList = new ObservableCollection<CompressOption>(compressOptions);
            var defaultCompressOption = compressOptions.Where(s => s.Type == mLoadedConfig.CompressOption).FirstOrDefault() ?? new CompressOption(CompressOptionType.None);
            mSelectedCompressOption = defaultCompressOption;

            var scrubOptions = Enum.GetValues(typeof(ScrubOptionType)).Cast<ScrubOptionType>().Select(e => new ScrubOption(e)).ToList();
            ScrubOptionList = new ObservableCollection<ScrubOption>(scrubOptions);
            var defaultScrubOption = scrubOptions.Where(s => s.Type == mLoadedConfig.ScrubOption).FirstOrDefault() ?? new ScrubOption(ScrubOptionType.None);
            mSelectedScrubOption = defaultScrubOption;

            mTraverseInputSubdirs = mLoadedConfig.RecurseInput;
            mDoNotSplitISO = mLoadedConfig.NoSplit;
            mInputFolder = mLoadedConfig.InputPath;
            mOutputFolder = mLoadedConfig.OutputPath;
            mUnpackFolder = mLoadedConfig.UnpackPath;

            mFilteredGameDataList = new ObservableCollection<GameData>(Array.Empty<GameData>());

            var searchObservables = new IObservable<object>[]
            {
                this.WhenAnyValue(x => x.SelectedGameDataSection).Select(v => (object)v),
                this.WhenAnyValue(x => x.SelectedGameDataFilter).Select(v => (object)v),
                this.WhenAnyValue(x => x.SearchForFilter).Select(v => (object)v),
                this.WhenAnyValue(x => x.ShowInvalid).Select(v => (object)v)
            };
            Observable.CombineLatest(searchObservables).DistinctUntilChanged().Subscribe(x => {
                OnSearchChanged();
            });

            var configObservables = new IObservable<object>[]
            {
                this.WhenAnyValue(x => x.SelectedGameDataSection).Select(v => (object)v),
                this.WhenAnyValue(x => x.SelectedGameDataFilter).Select(v => (object)v),
                this.WhenAnyValue(x => x.SelectedGroupingOption).Select(v => (object)v),
                this.WhenAnyValue(x => x.UseUppercase).Select(v => (object)v),
                this.WhenAnyValue(x => x.SelectedCompressOption).Select(v => (object)v),
                this.WhenAnyValue(x => x.SelectedScrubOption).Select(v => (object)v),
                this.WhenAnyValue(x => x.TraverseInputSubdirs).Select(v => (object)v),
                this.WhenAnyValue(x => x.DoNotSplitISO).Select(v => (object)v),
                this.WhenAnyValue(x => x.InputFolder).Select(v => (object)v),
                this.WhenAnyValue(x => x.OutputFolder).Select(v => (object)v),
                this.WhenAnyValue(x => x.UnpackFolder).Select(v => (object)v)
            };
            Observable.CombineLatest(configObservables).DistinctUntilChanged().Subscribe(x => {
                 OnConfigChanged();
             });
        }
    }
}

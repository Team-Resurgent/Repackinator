using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using System.Windows.Input;
using Tmds.DBus.Protocol;

namespace Repackinator;

public partial class MessageWindow : Window
{
    public ICommand OkCommand { get; }


    public MessageWindow()
    {
        InitializeComponent();

        Title = string.Empty;
        Message.Content = string.Empty;

        OkCommand = ReactiveCommand.Create(() =>
        {
            Close();
        });

        OkButton.Command = OkCommand;
    }

    public MessageWindow(string title, string message)
    {
        InitializeComponent();

        Title = title;
        Message.Content = message;

        OkCommand = ReactiveCommand.Create(() =>
        {
            Close();
        });

        OkButton.Command = OkCommand;
    }
}
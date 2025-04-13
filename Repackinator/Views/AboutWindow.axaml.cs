using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Diagnostics;
using System;
using ManagedBass.FftSignalProvider;
using ManagedBass;
using Repackinator.Core.Helpers;
using System.Reflection;

namespace Repackinator;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }
}
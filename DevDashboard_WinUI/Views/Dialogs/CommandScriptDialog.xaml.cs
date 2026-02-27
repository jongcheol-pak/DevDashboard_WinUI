using DevDashboard.Models;
using DevDashboard.Services;
using DevDashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinUIEx;

namespace DevDashboard.Views.Dialogs;

public sealed partial class CommandScriptDialog : WindowEx
{
    private const int MinW = 480;
    private const int InitW = 600;
    private const int InitH = 500;

    private CommandScriptDialogViewModel Vm { get; } = new();
    private readonly TaskCompletionSource _closedTcs = new();

    public CommandScript? ResultScript { get; private set; }

    public CommandScriptDialog(CommandScript? existing)
    {
        InitializeComponent();
        Title = LocalizationService.Get("CommandScriptDialogTitle");
        SystemBackdrop = new MicaBackdrop();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppTitleBarText.Text = Title;

        var manager = WindowManager.Get(this);
        manager.MinWidth = MinW;

        if (existing is not null)
            Vm.LoadFrom(existing);

        Closed += (_, _) => _closedTcs.TrySetResult();
    }

    internal Task ShowAsync()
    {
        DialogWindowHost.Show(this, InitW, InitH);
        return _closedTcs.Task;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var error = Vm.Validate();
        if (error is not null)
        {
            ErrorText.Text = error;
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        ErrorText.Visibility = Visibility.Collapsed;
        ResultScript = Vm.ToCommandScript();
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}

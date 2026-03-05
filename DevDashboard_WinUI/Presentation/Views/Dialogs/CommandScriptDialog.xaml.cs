using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinUIEx;

namespace DevDashboard.Presentation.Views.Dialogs;

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
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
        { p.IsMinimizable = false; p.IsMaximizable = false; }

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

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        try
        {
            var error = Vm.Validate();
            if (error is not null)
            {
                var dialog = new ContentDialog
                {
                    Title = LocalizationService.Get("InputRequired"),
                    Content = error,
                    CloseButtonText = LocalizationService.Get("Dialog_Close"),
                    XamlRoot = Content.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            ResultScript = Vm.ToCommandScript();
            Close();
        }
        catch (Exception ex)
        {
            await DialogService.ShowErrorAsync(
                string.Format(LocalizationService.Get("UnexpectedError"), ex.Message));
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}

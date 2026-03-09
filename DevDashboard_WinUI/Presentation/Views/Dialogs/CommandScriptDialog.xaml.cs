using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Presentation.Views.Dialogs;

public sealed partial class CommandScriptDialog : ContentDialog
{
    private CommandScriptDialogViewModel Vm { get; } = new();

    public CommandScript? ResultScript { get; private set; }

    public CommandScriptDialog(CommandScript? existing)
    {
        InitializeComponent();

        Title = LocalizationService.Get("CommandScriptDialogTitle");
        PrimaryButtonText = LocalizationService.Get("Dialog_Save");
        CloseButtonText = LocalizationService.Get("Dialog_Cancel");

        if (existing is not null)
            Vm.LoadFrom(existing);
    }

    internal new async Task<ContentDialogResult> ShowAsync()
    {
        XamlRoot = App.MainWindow?.Content?.XamlRoot;
        return await base.ShowAsync();
    }

    private void OnSave(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var error = Vm.Validate();
        if (error is not null)
        {
            ErrorText.Text = error;
            ErrorText.Visibility = Visibility.Visible;
            args.Cancel = true;
            return;
        }

        ResultScript = Vm.ToCommandScript();
    }
}

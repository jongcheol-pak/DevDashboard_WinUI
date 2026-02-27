using DevDashboard.Models;
using DevDashboard.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Views.Dialogs;

public sealed partial class CommandScriptDialog : ContentDialog
{
    private CommandScriptDialogViewModel Vm { get; } = new();

    public CommandScript? ResultScript { get; private set; }

    public CommandScriptDialog(CommandScript? existing)
    {
        InitializeComponent();
        if (existing is not null)
            Vm.LoadFrom(existing);
        PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var error = Vm.Validate();
        if (error is not null)
        {
            ErrorText.Text = error;
            ErrorText.Visibility = Visibility.Visible;
            args.Cancel = true;
            return;
        }

        ErrorText.Visibility = Visibility.Collapsed;
        ResultScript = Vm.ToCommandScript();
    }
}

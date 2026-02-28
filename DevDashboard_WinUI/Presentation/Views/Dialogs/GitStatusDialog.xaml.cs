using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinUIEx;

namespace DevDashboard.Presentation.Views.Dialogs;

public sealed partial class GitStatusDialog : WindowEx
{
    private const int MinW = 560;
    private const int InitW = 700;
    private const int InitH = 500;

    private readonly ProjectCardViewModel _card;
    private readonly TaskCompletionSource _closedTcs = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _initialized;

    public GitStatusDialog(ProjectCardViewModel card)
    {
        _card = card;
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
        { p.IsMinimizable = false; p.IsMaximizable = false; }

        var manager = WindowManager.Get(this);
        manager.MinWidth = MinW;

        // 프로젝트명을 포함한 동적 타이틀 설정
        Title = $"Git 상태 — {card.Name}";

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppTitleBarText.Text = Title;

        Closed += (_, _) =>
        {
            _cts.Cancel();
            _cts.Dispose();
            _closedTcs.TrySetResult();
        };
    }

    internal Task ShowAsync()
    {
        DialogWindowHost.Show(this, InitW, InitH);
        return _closedTcs.Task;
    }

    private async void OnRootLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;

        string? error;
        try
        {
            error = await _card.LoadGitStatusAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        LoadingPanel.Visibility = Visibility.Collapsed;

        if (error is not null)
        {
            ErrorBar.Message = error;
            ErrorBar.IsOpen = true;
            return;
        }

        BranchText.Text = _card.GitBranch;
        CommitList.ItemsSource = _card.GitChangedFiles;

        NoCommitsText.Visibility = _card.GitChangedFiles.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        ContentPanel.Visibility = Visibility.Visible;
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}

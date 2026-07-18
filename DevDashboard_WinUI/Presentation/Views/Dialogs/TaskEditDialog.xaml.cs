using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Presentation.Views.Dialogs;

/// <summary>새 작업/작업 편집 다이얼로그.</summary>
public sealed partial class TaskEditDialog : ContentDialog
{
    private TaskEditDialogViewModel Vm { get; }

    /// <summary>저장 시 확정된 작업 항목 (취소 시 null)</summary>
    public TodoItem? ResultTodo { get; private set; }

    /// <summary>"테스트 추가" 토글 선택 여부 (T9에서 테스트 자동 생성에 사용)</summary>
    public bool AddTestRequested { get; private set; }

    public TaskEditDialog(TodoItem? existing, AppSettings settings)
    {
        Vm = new TaskEditDialogViewModel(existing, settings);
        InitializeComponent();

        Title = existing is null
            ? LocalizationService.Get("TaskEdit_TitleAdd")
            : LocalizationService.Get("TaskEdit_TitleEdit");
        PrimaryButtonText = LocalizationService.Get("Dialog_Save");
        CloseButtonText = LocalizationService.Get("Dialog_Cancel");
    }

    internal new async Task<ContentDialogResult> ShowAsync()
    {
        XamlRoot = App.MainWindow?.Content?.XamlRoot;
        return await base.ShowAsync();
    }

    private void OnSave(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(Vm.Title))
        {
            ErrorText.Text = LocalizationService.Get("TaskEdit_TitleRequired");
            ErrorText.Visibility = Visibility.Visible;
            args.Cancel = true;
            return;
        }

        ResultTodo = Vm.BuildResult();
        AddTestRequested = Vm.AddTest;
    }
}

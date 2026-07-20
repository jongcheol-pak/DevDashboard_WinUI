using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
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

    /// <param name="status">
    /// 헤더 배지에 표시할 상태. 새 작업은 호출한 칸반 열의 상태를 넘긴다.
    /// 편집 모드는 이 값을 무시하고 기존 항목의 상태를 쓴다.
    /// </param>
    public TaskEditDialog(TodoItem? existing, AppSettings settings, TodoStatus status = TodoStatus.Waiting)
    {
        Vm = new TaskEditDialogViewModel(existing, settings, status);
        InitializeComponent();

        // 헤더 제목·상태 배지는 XAML의 ContentDialog.Title이 담당한다(여기서 Title을 대입하면 그것을 덮어쓴다).
        PrimaryButtonText = Vm.PrimaryButtonLabel;
        CloseButtonText = LocalizationService.Get("Dialog_Cancel");
    }

    internal new async Task<ContentDialogResult> ShowAsync()
    {
        XamlRoot = App.MainWindow?.Content?.XamlRoot;
        return await base.ShowAsync();
    }

    private void OnSave(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // 제목이 비면 저장을 막는다. 제목 입력칸 하단의 danger 라인은 값 유무와 무관하게 상시 표시라
        // "비어 있음" 신호로는 쓸 수 없으므로, 이 차단이 빈 제목 저장을 막는 유일한 방어선이다.
        if (string.IsNullOrWhiteSpace(Vm.Title))
        {
            args.Cancel = true;
            return;
        }

        ResultTodo = Vm.BuildResult();
        AddTestRequested = Vm.AddTest;
    }
}

using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DevDashboard.Presentation.Views.Dialogs;

/// <summary>새 작업/작업 편집 다이얼로그.</summary>
public sealed partial class TaskEditDialog : ContentDialog
{
    private TaskEditDialogViewModel Vm { get; }

    // 제목 입력칸 테두리 — x:Bind 함수 바인딩은 ThemeResource를 받을 수 없어
    // TaskPage.PriorityBrush와 동일하게 정적 브러시로 둔다(Palette.xaml 값과 수동으로 맞춘다).
    //   비어 있음 AppDangerColor(#F0716A) / 채워짐 AppBorderStrongColor(#2E2E34)
    private static readonly SolidColorBrush _titleRequiredBrush = new(ColorHelper.FromArgb(0xFF, 0xF0, 0x71, 0x6A));
    private static readonly SolidColorBrush _titleNormalBrush = new(ColorHelper.FromArgb(0xFF, 0x2E, 0x2E, 0x34));

    /// <summary>
    /// 제목이 비어 있으면 danger 테두리를 반환해 필수 입력을 상시 표시한다.
    /// 마우스를 올린 동안에는 WinUI 기본 hover 테두리(회색)가 이 값을 덮지만,
    /// 라벨의 빨간 * 가 항상 남아 있어 필수 신호는 유지된다.
    /// </summary>
    public static Brush TitleBorderBrush(string? title) =>
        string.IsNullOrWhiteSpace(title) ? _titleRequiredBrush : _titleNormalBrush;

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
        // 제목이 비면 저장을 막는다. 사용자에게는 제목 입력칸의 danger 테두리가 이미 표시돼 있어
        // 별도 오류 문구를 두지 않는다(TitleBorderBrush).
        if (string.IsNullOrWhiteSpace(Vm.Title))
        {
            args.Cancel = true;
            return;
        }

        ResultTodo = Vm.BuildResult();
        AddTestRequested = Vm.AddTest;
    }
}

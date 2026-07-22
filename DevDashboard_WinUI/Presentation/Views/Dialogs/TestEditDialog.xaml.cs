using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Presentation.Views.Dialogs;

/// <summary>새 테스트/테스트 편집 다이얼로그.</summary>
public sealed partial class TestEditDialog : ContentDialog
{
    private TestEditDialogViewModel Vm { get; }

    /// <summary>저장 시 확정된 테스트 항목 (취소 시 null)</summary>
    public TestItem? ResultTest { get; private set; }

    /// <summary>저장 시 선택/입력된 스위트 이름 (호출부가 스위트 연결에 사용)</summary>
    public string ResultSuiteName { get; private set; } = string.Empty;

    /// <param name="taskCategories">스위트로 고를 수 있는 작업 카테고리 목록</param>
    /// <param name="presetSuite">기본 선택 스위트 (편집이면 그 테스트의 현재 스위트)</param>
    public TestEditDialog(TestItem? existing, IReadOnlyList<string> taskCategories, string? presetSuite)
    {
        Vm = new TestEditDialogViewModel(existing, taskCategories, presetSuite);
        InitializeComponent();

        Title = existing is null
            ? LocalizationService.Get("TestEdit_TitleAdd")
            : LocalizationService.Get("TestEdit_TitleEdit");
        // 새 테스트는 "등록", 편집은 공용 "저장" (공용 Dialog_Save는 다른 다이얼로그와 공유하므로 값을 바꾸지 않는다)
        PrimaryButtonText = existing is null
            ? LocalizationService.Get("TestEdit_Submit")
            : LocalizationService.Get("Dialog_Save");
        CloseButtonText = LocalizationService.Get("Dialog_Cancel");
    }

    internal new async Task<ContentDialogResult> ShowAsync()
    {
        XamlRoot = App.MainWindow?.Content?.XamlRoot;
        return await base.ShowAsync();
    }

    private void OnSave(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(Vm.Name))
        {
            ShowError("TestEdit_NameRequired");
            args.Cancel = true;
            return;
        }
        if (string.IsNullOrWhiteSpace(Vm.SelectedSuite))
        {
            ShowError("TestEdit_SuiteRequired");
            args.Cancel = true;
            return;
        }

        ResultTest = Vm.BuildResult();
        ResultSuiteName = Vm.SelectedSuite.Trim();
    }

    private void ShowError(string resourceKey)
    {
        ErrorText.Text = LocalizationService.Get(resourceKey);
        ErrorText.Visibility = Visibility.Visible;
    }
}

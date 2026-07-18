using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DevDashboard.Presentation.Views;

/// <summary>테스트 전체 페이지 — 프로젝트 카드의 "테스트" 진입점에서 표시됩니다.</summary>
public sealed partial class TestPage : UserControl
{
    /// <summary>페이지 뷰모델 (x:Bind 대상)</summary>
    public TestPageViewModel Vm { get; }

    public TestPage(TestPageViewModel vm)
    {
        Vm = vm;
        InitializeComponent();
        DataContext = vm;
    }

    // ===== 상태 색·라벨 (PRD §3: 통과 #5aa3e8 / 실패 #e8b45a / 미실행 #8a8890) =====

    private static readonly SolidColorBrush _passBrush = new(ColorHelper.FromArgb(0xFF, 0x5A, 0xA3, 0xE8));
    private static readonly SolidColorBrush _failBrush = new(ColorHelper.FromArgb(0xFF, 0xE8, 0xB4, 0x5A));
    private static readonly SolidColorBrush _untestedBrush = new(ColorHelper.FromArgb(0xFF, 0x8A, 0x88, 0x90));

    public static Brush PassBrush => _passBrush;
    public static Brush FailBrush => _failBrush;
    public static Brush UntestedBrush => _untestedBrush;

    public static string LabelAll { get; } = LocalizationService.Get("TestStatus_All");
    public static string LabelPass { get; } = LocalizationService.Get("TestStatus_Pass");
    public static string LabelFail { get; } = LocalizationService.Get("TestStatus_Fail");
    public static string LabelUntested { get; } = LocalizationService.Get("TestStatus_Untested");

    // 헤더·툴팁 (x:Bind 정적 참조 — 뒤로가기·수정·삭제는 작업 페이지 리소스 재사용)
    public static string PageTitle { get; } = LocalizationService.Get("TestPage_Title");
    public static string AddButtonText { get; } = LocalizationService.Get("TestAdd_Button");
    public static string BackText { get; } = LocalizationService.Get("TestPage_Back");
    public static string EditTooltip { get; } = LocalizationService.Get("TaskEdit_Tooltip");
    public static string DeleteTooltip { get; } = LocalizationService.Get("TaskDelete_Tooltip");
    public static string NoteTooltip { get; } = LocalizationService.Get("TestNote_Tooltip");

    /// <summary>테스트 상태 콤보박스 항목 (통과/실패/미실행)</summary>
    public static IReadOnlyList<TestStatusOption> StatusOptions { get; } =
    [
        new(TestItem.StatusPass, LocalizationService.Get("TestStatus_Pass")),
        new(TestItem.StatusFail, LocalizationService.Get("TestStatus_Fail")),
        new(TestItem.StatusUntested, LocalizationService.Get("TestStatus_Untested")),
    ];

    // ===== x:Bind 정적 헬퍼 (Converter 미적용 — 직접 타입 반환) =====

    /// <summary>상태 색 브러시 (아이콘·배지용)</summary>
    public static Brush StatusBrush(string status) => status switch
    {
        TestItem.StatusPass => _passBrush,
        TestItem.StatusFail => _failBrush,
        _ => _untestedBrush,
    };

    /// <summary>상태 아이콘 글리프 (통과 ✓ / 실패 ✕ / 미실행 ○)</summary>
    public static string StatusGlyph(string status) => status switch
    {
        TestItem.StatusPass => "✓",
        TestItem.StatusFail => "✕",
        _ => "○",
    };

    /// <summary>메모 표시 여부 (메모가 있을 때만)</summary>
    public static Visibility NoteVisibility(string note)
        => string.IsNullOrWhiteSpace(note) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>방법 표시 여부 (방법이 있을 때만)</summary>
    public static Visibility MethodVisibility(string method)
        => string.IsNullOrWhiteSpace(method) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>통과율 텍스트 ("N% (통과/전체)")</summary>
    public static string FormatPassRate(double rate) => $"{rate:0}%";

    // ===== 네비게이션 =====

    private void Back_Click(object sender, RoutedEventArgs e)
        => (App.MainWindow as MainWindow)?.ShowDashboard();

    // ===== 상태 필터 탭 =====

    private void StatusTab_Checked(object sender, RoutedEventArgs e)
    {
        // Vm은 InitializeComponent 이후 설정되므로 최초 로드 시 null 가드
        if (sender is not RadioButton { Tag: string tag } || Vm is null) return;
        Vm.SelectedStatus = string.IsNullOrEmpty(tag) ? null : tag;
    }

    // ===== 항목 상태 콤보 =====

    private void StatusCombo_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox { Tag: TestItem test } combo) return;
        combo.ItemsSource = StatusOptions;
        combo.SelectedItem = StatusOptions.FirstOrDefault(o => o.Value == test.Status);
    }

    private void StatusCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { Tag: TestItem test, SelectedItem: TestStatusOption option }) return;
        // ChangeTestStatus는 동일 상태면 무시하므로 Loaded 초기 선택으로 인한 불필요한 저장이 없다.
        Vm.ChangeTestStatus(test, option.Value);
    }

    // ===== 테스트 추가/편집/삭제/메모 =====

    private async void AddTest_Click(object sender, RoutedEventArgs e)
    {
        var suiteNames = SuiteNames();
        var presetSuite = suiteNames.Count > 0 ? suiteNames[0] : null;
        var dialog = new Dialogs.TestEditDialog(null, suiteNames, presetSuite);
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && dialog.ResultTest is { } test)
            Vm.AddTestToSuite(dialog.ResultSuiteName, test);
    }

    private async void EditTest_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TestItem test }) return;
        var currentSuite = Vm.Project.TestCategories?.FirstOrDefault(c => c.Id == test.CategoryId)?.Name;
        var dialog = new Dialogs.TestEditDialog(test, SuiteNames(), currentSuite);
        // 편집은 in-place 수정이므로 UpdateTest만 호출한다(중복 추가 방지 — add/edit 경로 분리, S1).
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && dialog.ResultTest is not null)
            Vm.UpdateTest(test);
    }

    private async void DeleteTest_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TestItem test }) return;
        var confirmed = await DialogService.ShowConfirmAsync(
            LocalizationService.Get("DeleteConfirmMessage"),
            LocalizationService.Get("DeleteConfirmTitle"));
        if (confirmed)
            Vm.DeleteTest(test);
    }

    private async void EditNote_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TestItem test }) return;

        var textBox = new TextBox
        {
            Text = test.ProgressNote,
            MaxLength = 500,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            Height = 120,
        };
        var dialog = new ContentDialog
        {
            Title = LocalizationService.Get("TestEditNoteTitle"),
            Content = textBox,
            PrimaryButtonText = LocalizationService.Get("Dialog_Save"),
            CloseButtonText = LocalizationService.Get("Dialog_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = App.MainWindow?.Content?.XamlRoot,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            Vm.EditProgressNote(test, textBox.Text);
    }

    // ===== 스위트 이름수정/삭제 =====

    private async void RenameSuite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TestCategory suite }) return;

        var textBox = new TextBox { Text = suite.Name, MaxLength = 100 };
        var dialog = new ContentDialog
        {
            Title = LocalizationService.Get("TestEditCategoryTitle"),
            Content = textBox,
            PrimaryButtonText = LocalizationService.Get("Dialog_Save"),
            CloseButtonText = LocalizationService.Get("Dialog_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = App.MainWindow?.Content?.XamlRoot,
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
            Vm.RenameSuite(suite, textBox.Text);
    }

    private async void DeleteSuite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TestCategory suite }) return;
        var confirmed = await DialogService.ShowConfirmAsync(
            LocalizationService.Get("TestDeleteCategoryConfirm"),
            LocalizationService.Get("DeleteConfirmTitle"));
        if (confirmed)
            Vm.DeleteSuite(suite);
    }

    /// <summary>현재 프로젝트의 스위트 이름 목록 (다이얼로그 ComboBox 소스)</summary>
    private IReadOnlyList<string> SuiteNames()
        => Vm.Project.TestCategories?.Select(c => c.Name).ToList() ?? [];
}

/// <summary>테스트 상태 콤보박스 항목</summary>
public sealed record TestStatusOption(string Value, string DisplayName)
{
    public override string ToString() => DisplayName;
}

using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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

        // 스위트 필터 콤보 구성 (전체 + 작업 카테고리) — TaskPage 카테고리 필터와 같은 방식
        var suiteOptions = new List<string> { LocalizationService.Get("TestSuiteFilter_All") };
        suiteOptions.AddRange(vm.AvailableCategories);
        SuiteFilterCombo.ItemsSource = suiteOptions;
        SuiteFilterCombo.SelectedIndex = 0;
    }

    // ===== 상태 색·라벨 (PRD §3: 통과 #5aa3e8 / 실패 #e8b45a / 미실행 #8a8890) =====

    private static readonly SolidColorBrush _passBrush = new(ColorHelper.FromArgb(0xFF, 0x5A, 0xA3, 0xE8));
    private static readonly SolidColorBrush _failBrush = new(ColorHelper.FromArgb(0xFF, 0xE8, 0xB4, 0x5A));
    private static readonly SolidColorBrush _untestedBrush = new(ColorHelper.FromArgb(0xFF, 0x8A, 0x88, 0x90));

    // 상태 pill·아이콘 배경용 저투명 버전 (Palette의 *SoftBrush와 같은 0x28 알파 규약)
    private static readonly SolidColorBrush _passSoftBrush = new(ColorHelper.FromArgb(0x28, 0x5A, 0xA3, 0xE8));
    private static readonly SolidColorBrush _failSoftBrush = new(ColorHelper.FromArgb(0x28, 0xE8, 0xB4, 0x5A));
    private static readonly SolidColorBrush _untestedSoftBrush = new(ColorHelper.FromArgb(0x28, 0x8A, 0x88, 0x90));

    // 상태색으로 채운 아이콘 위의 글리프 색 / 테두리·배경을 그리지 않을 때 쓰는 투명 브러시
    private static readonly SolidColorBrush _glyphOnFillBrush = new(Colors.White);
    private static readonly SolidColorBrush _transparentBrush = new(Colors.Transparent);

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

    /// <summary>상태 pill 툴팁 — 행에 조작 버튼이 없으므로 클릭으로 상태가 바뀐다는 것을 알린다.</summary>
    public static string StatusPillTooltip { get; } = LocalizationService.Get("TestStatusPill_Tooltip");

    // ===== x:Bind 정적 헬퍼 (Converter 미적용 — 직접 타입 반환) =====

    /// <summary>상태 색 브러시 (아이콘·배지용)</summary>
    public static Brush StatusBrush(string status) => status switch
    {
        TestItem.StatusPass => _passBrush,
        TestItem.StatusFail => _failBrush,
        _ => _untestedBrush,
    };

    /// <summary>상태 배지·아이콘 배경용 저투명 브러시</summary>
    public static Brush StatusSoftBrush(string status) => status switch
    {
        TestItem.StatusPass => _passSoftBrush,
        TestItem.StatusFail => _failSoftBrush,
        _ => _untestedSoftBrush,
    };

    /// <summary>상태 아이콘 배경 — 통과·실패는 상태색으로 채우고, 미실행은 저투명(테두리로 표현)</summary>
    public static Brush StatusIconBackground(string status) => status switch
    {
        TestItem.StatusPass => _passBrush,
        TestItem.StatusFail => _failBrush,
        _ => _untestedSoftBrush,
    };

    /// <summary>상태 아이콘 테두리 — 채워지지 않는 미실행만 회색 테두리를 그린다</summary>
    public static Brush StatusIconBorderBrush(string status) => status switch
    {
        TestItem.StatusPass or TestItem.StatusFail => _transparentBrush,
        _ => _untestedBrush,
    };

    /// <summary>상태 아이콘 테두리 두께 — Border의 배경은 테두리 안쪽에만 그려지므로,
    /// 채움 상태에 두께를 남기면 색 사각형이 그만큼 작아져 미실행과 크기가 달라 보인다.</summary>
    public static Thickness StatusIconBorderThickness(string status) => status switch
    {
        TestItem.StatusPass or TestItem.StatusFail => new Thickness(0),
        _ => new Thickness(1.5),
    };

    /// <summary>상태 아이콘 글리프 색 — 채워진 배경 위에서는 흰색, 미실행은 회색</summary>
    public static Brush StatusGlyphForeground(string status) => status switch
    {
        TestItem.StatusPass or TestItem.StatusFail => _glyphOnFillBrush,
        _ => _untestedBrush,
    };

    /// <summary>메모 블록 표시 여부 (메모가 있을 때만 행 아래에 붙는다)</summary>
    public static Visibility NoteVisibility(string note)
        => string.IsNullOrWhiteSpace(note) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>상태 아이콘 글리프 (통과 ✓ / 실패 ✕ / 미실행 ○)</summary>
    public static string StatusGlyph(string status) => status switch
    {
        TestItem.StatusPass => "✓",
        TestItem.StatusFail => "✕",
        _ => "○",
    };

    /// <summary>상태 표시 텍스트 (통과/실패/미실행)</summary>
    public static string StatusText(string status) => status switch
    {
        TestItem.StatusPass => LabelPass,
        TestItem.StatusFail => LabelFail,
        _ => LabelUntested,
    };

    /// <summary>스위트 헤더의 통과 현황 텍스트 ("통과수/전체수 통과")</summary>
    public static string FormatPassCount(int passCount, int totalCount)
        => string.Format(LocalizationService.Get("TestSuitePassCount"), passCount, totalCount);

    /// <summary>진행바 전체 폭 — XAML의 트랙 Grid Width와 같은 값이어야 인디케이터 비율이 맞는다.</summary>
    private const double ProgressBarWidth = 120d;

    /// <summary>통과율(0~100)에 해당하는 진행바 인디케이터 폭</summary>
    public static double IndicatorWidth(double passRate)
        => ProgressBarWidth * Math.Clamp(passRate, 0d, 100d) / 100d;

    // ===== 네비게이션 =====

    private void Back_Click(object sender, RoutedEventArgs e)
        => (App.MainWindow as MainWindow)?.ShowDashboard();

    // ===== 상태 필터 탭 =====

    /// <summary>전체 탭의 Tag 값 — 빈 문자열을 쓰면 XAML 파싱 결과에 따라 Tag가 null이 되어
    /// 필터 해제가 통째로 무시될 수 있으므로 명시값을 쓴다.</summary>
    private const string StatusTagAll = "All";

    private void StatusTab_Checked(object sender, RoutedEventArgs e)
    {
        // 생성자에서 Vm이 InitializeComponent보다 먼저 설정되므로, XAML 파싱 중 초기 IsChecked=True 발화 시에도 Vm은 non-null이다.
        if (sender is not RadioButton radio) return;
        // Tag를 패턴 매칭으로 받으면 null일 때 조용히 빠져나가 직전 필터가 남는다 — null·빈 문자열·"All"을 모두 전체로 본다.
        var tag = radio.Tag as string;
        Vm.SelectedStatus = string.IsNullOrEmpty(tag) || tag == StatusTagAll ? null : tag;
    }

    // ===== 스위트(작업 카테고리) 필터 =====

    private void SuiteFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 첫 항목은 "전체"(SelectedIndex 0)이므로 필터를 해제한다.
        if (sender is not ComboBox { SelectedIndex: var index } combo) return;
        Vm.SelectedSuiteFilter = index <= 0 ? null : combo.SelectedItem as string;
    }

    // ===== 항목 행 마우스 오버 =====
    // DataTemplate 안에서는 VisualStateManager의 GoToState가 동작하지 않으므로 배경을 직접 바꾼다.

    private void Row_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border row) row.Background = (Brush)Resources["RowHoverBrush"];
    }

    private void Row_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        // null이 아니라 Transparent로 되돌린다 — null이면 hit-test 영역이 텍스트로 좁아져 행 hover가 끊긴다.
        if (sender is Border row) row.Background = _transparentBrush;
    }

    // ===== 항목 상태 pill (클릭하면 다음 상태로 순환) =====

    /// <summary>다음 상태 (통과 → 실패 → 미실행 → 통과)</summary>
    private static string NextStatus(string status) => status switch
    {
        TestItem.StatusPass => TestItem.StatusFail,
        TestItem.StatusFail => TestItem.StatusUntested,
        _ => TestItem.StatusPass,
    };

    private void StatusPill_Click(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TestItem test }) return;
        // 상위 행으로 전파되면 우클릭 메뉴·행 조작과 겹치므로 여기서 소비한다.
        e.Handled = true;
        Vm.ChangeTestStatus(test, NextStatus(test.Status));
    }

    // ===== 테스트 추가/편집/삭제/메모 =====

    private async void AddTest_Click(object sender, RoutedEventArgs e)
    {
        // 스위트는 작업 카테고리에서 고른다 — 스위트 이름이 작업 카테고리와 같아야 칸반 통과율 배지에 반영된다.
        var categories = Vm.AvailableCategories;
        var presetSuite = categories.Count > 0 ? categories[0] : null;
        var dialog = new Dialogs.TestEditDialog(null, categories, presetSuite);
        if (await dialog.ShowAsync() == ContentDialogResult.Primary && dialog.ResultTest is { } test)
            Vm.AddTestToSuite(dialog.ResultSuiteName, test);
    }

    private async void EditTest_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: TestItem test }) return;
        var currentSuite = Vm.Project.TestCategories?.FirstOrDefault(c => c.Id == test.CategoryId)?.Name;
        var dialog = new Dialogs.TestEditDialog(test, Vm.AvailableCategories, currentSuite);
        // 편집은 in-place 수정이므로 UpdateTest만 호출한다(신규 추가 경로 AddTestToSuite와 분리 — 기존 항목 중복 추가 방지).
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

        var dialog = new Dialogs.TestNoteDialog(test.Text, test.ProgressNote);
        // 메모를 비운 채 저장하면 메모가 삭제된다(다이얼로그 안내 문구와 같은 동작).
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            Vm.EditProgressNote(test, dialog.ResultNote);
    }

}

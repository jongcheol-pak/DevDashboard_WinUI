using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace DevDashboard.Presentation.Views.Dialogs;

public sealed partial class HistoryDialog : ContentDialog
{
    private HistoryDialogViewModel Vm { get; }

    // ===== 유형 배지 색 (시안 :1375~1379 값) =====
    // x:Bind 함수 바인딩은 Converter·ThemeResource를 받지 못하므로 Brush를 직접 반환한다(TestPage 선례).
    // 팔레트에 없는 색(특히 리팩터링 보라)이 섞여 있으나 전역 강조색이 아니라 유형 구분색이다.
    private static readonly SolidColorBrush _kindFeatureBrush = new(ColorHelper.FromArgb(0xFF, 0x7A, 0xB5, 0xEC));
    private static readonly SolidColorBrush _kindFixBrush = new(ColorHelper.FromArgb(0xFF, 0xE8, 0xB4, 0x5A));
    private static readonly SolidColorBrush _kindRefactorBrush = new(ColorHelper.FromArgb(0xFF, 0xB7, 0x9A, 0xE8));
    private static readonly SolidColorBrush _kindDefaultBrush = new(ColorHelper.FromArgb(0xFF, 0x8A, 0x88, 0x90));

    private static readonly SolidColorBrush _kindFeatureBackBrush = new(ColorHelper.FromArgb(0x29, 0x5A, 0xA3, 0xE8));
    private static readonly SolidColorBrush _kindFixBackBrush = new(ColorHelper.FromArgb(0x29, 0xE8, 0xB4, 0x5A));
    private static readonly SolidColorBrush _kindRefactorBackBrush = new(ColorHelper.FromArgb(0x38, 0x7A, 0x4A, 0xC0));
    private static readonly SolidColorBrush _kindDefaultBackBrush = new(ColorHelper.FromArgb(0xFF, 0x2B, 0x2B, 0x31));

    /// <summary>유형 배지 글자색. 기본 유형 외(사용자 정의 포함)는 시안대로 회색으로 떨어진다.</summary>
    public static Brush KindBrush(string kind) => kind switch
    {
        "기능" => _kindFeatureBrush,
        "수정" => _kindFixBrush,
        "리팩터링" => _kindRefactorBrush,
        _ => _kindDefaultBrush,
    };

    /// <summary>유형 배지 배경색 (글자색과 같은 계열의 저투명 틴트)</summary>
    public static Brush KindBackBrush(string kind) => kind switch
    {
        "기능" => _kindFeatureBackBrush,
        "수정" => _kindFixBackBrush,
        "리팩터링" => _kindRefactorBackBrush,
        _ => _kindDefaultBackBrush,
    };

    /// <summary>헤더의 총 건수 표시 ("N건")</summary>
    public static string CountText(int count)
        => string.Format(LocalizationService.Get("HistoryDialog_CountFormat"), count);

    /// <summary>번호 버튼 표시 여부 (생략 표식 자리에는 버튼을 두지 않는다)</summary>
    public static Visibility NumberVisibility(bool isEllipsis)
        => isEllipsis ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>현재 페이지만 액센트로 강조한다</summary>
    public static Style PageButtonStyle(bool isCurrent)
        => (Style)Application.Current.Resources[isCurrent ? "AccentButtonStyle" : "DefaultButtonStyle"];
    private readonly System.ComponentModel.PropertyChangedEventHandler _vmPropertyChangedHandler;
    private TaskCompletionSource<bool>? _nestedTcs;

    /// <summary>폼이 수정 중인 기록. null이면 새 기록 입력 상태</summary>
    private HistoryEntryViewModel? _editingEntry;

    public HistoryDialog(HistoryDialogViewModel vm)
    {
        Vm = vm;
        InitializeComponent();

        // 제목은 본문 헤더(제목 + 프로젝트 pill + 건수)가 담당하므로 ContentDialog.Title은 쓰지 않는다
        CloseButtonText = LocalizationService.Get("Dialog_Close");

        BackText.Text = LocalizationService.Get("HistoryForm_Back");

        GroupList.ItemsSource = Vm.DateGroups;
        _vmPropertyChangedHandler = (_, _) => RefreshList();
        Vm.PropertyChanged += _vmPropertyChangedHandler;
        RefreshList();

        Closing += (_, _) =>
        {
            Vm.PropertyChanged -= _vmPropertyChangedHandler;
        };
    }

    internal new async Task<ContentDialogResult> ShowAsync()
    {
        XamlRoot = App.MainWindow?.Content?.XamlRoot;
        ContentDialogResult result;
        do
        {
            result = await base.ShowAsync();
            if (_nestedTcs is not null)
            {
                await _nestedTcs.Task;
                _nestedTcs = null;
                continue;
            }
            break;
        } while (true);
        return result;
    }

    private async Task<ContentDialogResult> ShowNestedDialogAsync(ContentDialog dialog)
    {
        _nestedTcs = new TaskCompletionSource<bool>();
        Hide();
        dialog.XamlRoot = App.MainWindow?.Content?.XamlRoot;
        try
        {
            return await dialog.ShowAsync();
        }
        finally
        {
            _nestedTcs.TrySetResult(true);
        }
    }

    private Task ShowNestedErrorAsync(string message)
    {
        var dialog = new ContentDialog
        {
            Title = LocalizationService.Get("Dialog_DefaultErrorTitle"),
            Content = string.Format(LocalizationService.Get("UnexpectedError"), message),
            CloseButtonText = LocalizationService.Get("Dialog_OK"),
        };
        return ShowNestedDialogAsync(dialog);
    }

    /// <summary>추가 폼을 열고 제목을 미리 채웁니다. 작업을 완료로 옮길 때 작업 페이지에서 호출합니다.</summary>
    internal void OpenAddPanel(string initialTitle) => ShowFormView(null, initialTitle);

    // ===== 목록 ↔ 폼 전환 =====

    /// <summary>기록 입력 폼을 엽니다. editing이 null이면 새 기록, 아니면 그 기록을 수정합니다.</summary>
    private void ShowFormView(HistoryEntryViewModel? editing, string initialTitle)
    {
        _editingEntry = editing;

        FormTitleBox.Text = editing?.Model.Title ?? initialTitle;
        FormDetailBox.Text = editing?.Model.Description ?? string.Empty;

        // 새 기록은 지금 시각을 기본값으로 둔다 (대부분 방금 끝낸 일을 적기 때문)
        var when = editing?.Model.CompletedAt ?? DateTime.Now;
        FormDatePicker.Date = new DateTimeOffset(when.Date);
        FormTimePicker.Time = when.TimeOfDay;
        InitKindCombo(FormKindCombo, editing?.Model.Kind);

        FormHeaderText.Text = editing is null
            ? LocalizationService.Get("HistoryForm_AddHeader")
            : LocalizationService.Get("HistoryEditHeader");
        UpdateSaveEnabled();

        // 목록·검색·페이지네이션과 하단 [닫기]를 함께 감춰 폼만 남긴다(시안은 폼 화면에 닫기가 없다)
        ListArea.Visibility = Visibility.Collapsed;
        FormArea.Visibility = Visibility.Visible;
        CloseButtonText = string.Empty;
    }

    /// <summary>폼을 닫고 목록으로 돌아갑니다 (입력 중이던 내용은 버립니다).</summary>
    private void ShowListView()
    {
        _editingEntry = null;
        FormArea.Visibility = Visibility.Collapsed;
        ListArea.Visibility = Visibility.Visible;
        CloseButtonText = LocalizationService.Get("Dialog_Close");
    }

    /// <summary>제목이 비면 저장할 수 없다 — 시안은 오류 문구 대신 버튼을 비활성으로 알린다.</summary>
    private void UpdateSaveEnabled()
        => FormSaveButton.IsEnabled = !string.IsNullOrWhiteSpace(FormTitleBox.Text);

    private void FormTitle_TextChanged(object sender, TextChangedEventArgs e) => UpdateSaveEnabled();

    private void BackToList_Click(object sender, RoutedEventArgs e) => ShowListView();

    private void PageNumber_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int page })
            Vm.GoToPageCommand.Execute(page);
    }

    // ObservableCollection이 CollectionChanged로 자동 갱신하므로 ItemsSource 재할당 불필요
    private void RefreshList()
    {
        // 보이는 목록이 비었을 때 안내한다. 검색 때문에 빈 것과 기록 자체가 없는 것은
        // 사용자가 해야 할 일이 다르므로(검색어를 지우기 vs 기록을 추가하기) 문구를 나눈다.
        var isEmpty = Vm.FilteredCount == 0;
        EmptyText.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        if (isEmpty)
        {
            EmptyText.Text = string.IsNullOrWhiteSpace(Vm.SearchText)
                ? LocalizationService.Get("HistoryEmpty_NoEntries")
                : LocalizationService.Get("HistoryEmpty_NoSearchResult");
        }
    }

    /// <summary>유형 콤보박스를 "미분류" + 사용자 유형 목록으로 구성하고 선택값을 설정합니다.</summary>
    private void InitKindCombo(ComboBox combo, string? selectedKind)
    {
        var items = new List<string> { LocalizationService.Get("HistoryKind_None") };
        items.AddRange(Vm.AvailableKinds);
        combo.ItemsSource = items;
        combo.SelectedItem = !string.IsNullOrEmpty(selectedKind) && items.Contains(selectedKind)
            ? selectedKind
            : items[0];
    }

    /// <summary>유형 콤보박스 선택값을 반환합니다 ("미분류"·미선택은 빈 문자열).</summary>
    private string KindFromCombo(ComboBox combo)
    {
        var kind = combo.SelectedItem as string;
        return string.IsNullOrEmpty(kind) || kind == LocalizationService.Get("HistoryKind_None")
            ? string.Empty
            : kind;
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            Vm.SearchText = sender.Text;
    }

    private void ToggleAddPanel_Click(object sender, RoutedEventArgs e) => ShowFormView(null, string.Empty);

    // 편집도 같은 폼을 쓴다 — 중첩 다이얼로그로는 날짜·시각을 고칠 자리가 없었다
    private void EditEntry_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: HistoryEntryViewModel entryVm })
            ShowFormView(entryVm, string.Empty);
    }

    private void SaveForm_Click(object sender, RoutedEventArgs e)
    {
        var title = FormTitleBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title)) return;   // 저장 버튼이 비활성이라 도달하지 않지만 방어

        var date = FormDatePicker.Date?.Date ?? DateTime.Today;
        var completedAt = date + FormTimePicker.Time;
        var desc = FormDetailBox.Text.Trim();
        var kind = KindFromCombo(FormKindCombo);

        if (_editingEntry is null)
            Vm.AddEntry(new HistoryEntry { Title = title, Description = desc, Kind = kind, CompletedAt = completedAt });
        else
            Vm.UpdateEntry(_editingEntry, title, desc, kind, completedAt);

        ShowListView();
    }

    private void DeleteEntry_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: HistoryEntryViewModel entryVm })
            Vm.DeleteEntryCommand.Execute(entryVm);
    }

    // 항목 Border 클릭 → 펼침/접힘 토글
    private void EntryBorder_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is Border { Tag: HistoryEntryViewModel entryVm })
            entryVm.ToggleExpandCommand.Execute(null);
    }

    // 행 hover 배경 — DataTemplate 안의 Border라 VisualState가 자동 공급되지 않아 직접 교체한다
    private void EntryBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
            border.Background = (Brush)Application.Current.Resources["AppHoverFillBrush"];
    }

    private void EntryBorder_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
            border.Background = (Brush)Application.Current.Resources["AppCardAltBrush"];
    }

    // 수정 버튼 Tapped → 부모 Border의 Tapped 이벤트 전파 차단
    private void EditBtn_Tapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }

    // 삭제 버튼 Tapped → 부모 Border의 Tapped 이벤트 전파 차단
    private void DeleteBtn_Tapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileSavePicker();
            picker.FileTypeChoices.Add(LocalizationService.Get("HistoryDialog_MarkdownType"), [".md"]);
            picker.SuggestedFileName = $"{Vm.ProjectName}_history";

            // FileSavePicker hwnd는 MainWindow를 사용
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file is not null)
                await FileIO.WriteTextAsync(file, Vm.ExportToMarkdown());
        }
        catch (Exception ex)
        {
            await ShowNestedErrorAsync(ex.Message);
        }
    }
}

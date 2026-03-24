using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Text;

namespace DevDashboard.Presentation.Views.Dialogs;

/// <summary>
/// 테스트 목록 다이얼로그.
/// 카테고리(TestCategory) 기반 중첩 구조로 테스트 항목을 표시합니다.
/// </summary>
public sealed partial class TestListDialog : ContentDialog
{
    private TestListDialogViewModel Vm { get; }

    /// <summary>초기화 완료 플래그 — 목록 바인딩 시 이벤트 핸들러 무시</summary>
    private bool _isRefreshing;

    /// <summary>현재 선택된 탭 — GetAddTestPanelVisibility에서 참조</summary>
    private static string _currentTab = "Testing";

    /// <summary>중첩 다이얼로그 완료 대기용 TCS</summary>
    private TaskCompletionSource<bool>? _nestedTcs;

    public TestListDialog(TestListDialogViewModel vm)
    {
        Vm = vm;
        InitializeComponent();

        Title = LocalizationService.Get("TestListDialogTitle");
        CloseButtonText = LocalizationService.Get("Dialog_Close");

        RefreshList();
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

    /// <summary>현재 다이얼로그를 숨기고 중첩 다이얼로그를 표시한 후 재표시합니다.</summary>
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

    // --- x:Bind 함수 바인딩용 정적 헬퍼 ---

    /// <summary>상태에 따른 텍스트 장식 (완료 시 취소선)</summary>
    public static TextDecorations GetTextDecorations(string status)
        => status == TestItem.StatusDone ? TextDecorations.Strikethrough : TextDecorations.None;

    /// <summary>상태에 따른 투명도</summary>
    public static double GetOpacity(string status)
        => status == TestItem.StatusDone ? 0.5 : 1.0;

    /// <summary>완료 상태가 아닌지 판별 (함수 바인딩용)</summary>
    public static bool IsNotDone(string status) => status != TestItem.StatusDone;

    /// <summary>Status 값을 ComboBox 인덱스로 변환 (함수 바인딩용)</summary>
    public static int GetStatusIndex(string status) => status switch
    {
        TestItem.StatusTesting => 0,
        TestItem.StatusFix => 1,
        TestItem.StatusDone => 2,
        _ => 0
    };

    /// <summary>전체 탭에서 수정 상태 항목의 제목 색상 (빨간색)</summary>
    public static Microsoft.UI.Xaml.Media.Brush GetTextForeground(string status)
    {
        if (status == TestItem.StatusFix && _currentTab == "All")
            return new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        return (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["TextFillColorPrimaryBrush"];
    }

    /// <summary>시작 날짜 포맷 (함수 바인딩용)</summary>
    public static string FormatCreatedAt(DateTime createdAt)
        => $"{LocalizationService.Get("TestLabel_Start")} {createdAt:yyyy-MM-dd HH:mm}";

    /// <summary>완료 날짜 포맷 (함수 바인딩용)</summary>
    public static string FormatCompletedAt(DateTime? completedAt)
        => completedAt is null
            ? string.Empty
            : $"{LocalizationService.Get("TestLabel_Completed")} {completedAt:yyyy-MM-dd HH:mm}";

    /// <summary>완료 날짜 표시 여부 (함수 바인딩용)</summary>
    public static Visibility GetCompletedVisibility(string status)
        => status == TestItem.StatusDone ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>진행 내용 표시 여부 — 메모가 있으면 항상 표시</summary>
    public static Visibility GetNoteVisibility(string status, string progressNote)
        => string.IsNullOrWhiteSpace(progressNote) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>메모 추가 링크 표시 여부 — 미완료이고 메모가 없을 때</summary>
    public static Visibility GetAddNoteVisibility(string status, string progressNote)
        => status != TestItem.StatusDone && string.IsNullOrWhiteSpace(progressNote) ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>카테고리 내 항목 추가 패널 표시 여부 — 완료 탭에서는 숨김</summary>
    public static Visibility GetAddTestPanelVisibility()
        => _currentTab == "Done" ? Visibility.Collapsed : Visibility.Visible;

    private void RefreshList()
    {
        _isRefreshing = true;
        CategoryList.ItemsSource = null;
        CategoryList.ItemsSource = Vm.FilteredCategories;
        EmptyText.Visibility = Vm.FilteredCategories.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        AddCategoryPanel.Visibility = Vm.SelectedTab == "Testing" ? Visibility.Visible : Visibility.Collapsed;
        _isRefreshing = false;
    }

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string tab })
        {
            _currentTab = tab;
            Vm.ChangeTabCommand.Execute(tab);
            RefreshList();
        }
    }

    private void AddCategory_Click(object sender, RoutedEventArgs e)
    {
        Vm.NewCategoryName = NewCategoryBox.Text;
        Vm.AddCategoryCommand.Execute(null);
        NewCategoryBox.Text = string.Empty;
        RefreshList();
    }

    private void AddTestToCategory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TestCategory category }) return;

        // 같은 카테고리의 TextBox 찾기 — Button의 부모 Grid에서 TextBox 탐색
        var button = (Button)sender;
        if (button.Parent is Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (child is TextBox textBox && textBox.Tag is TestCategory tbCat && tbCat.Id == category.Id)
                {
                    var text = textBox.Text?.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        Vm.AddTestToCategory(category, text);
                        textBox.Text = string.Empty;
                    }
                    break;
                }
            }
        }
        RefreshList();
    }

    private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button { Tag: TestCategory category }) return;

            var dialog = new ContentDialog
            {
                Title = LocalizationService.Get("DeleteConfirmTitle"),
                Content = LocalizationService.Get("TestDeleteCategoryConfirm"),
                PrimaryButtonText = LocalizationService.Get("Dialog_Delete"),
                CloseButtonText = LocalizationService.Get("Dialog_Cancel"),
                DefaultButton = ContentDialogButton.Close,
            };

            if (await ShowNestedDialogAsync(dialog) != ContentDialogResult.Primary) return;

            Vm.DeleteCategoryCommand.Execute(category);
            RefreshList();
        }
        catch (Exception ex)
        {
            await ShowNestedErrorAsync(ex.Message);
        }
    }

    private async void EditCategory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button { Tag: TestCategory category }) return;

            var textBox = new TextBox { Text = category.Name, MaxLength = 100 };

            var dialog = new ContentDialog
            {
                Title = LocalizationService.Get("TestEditCategoryTitle"),
                Content = textBox,
                PrimaryButtonText = LocalizationService.Get("Dialog_Save"),
                CloseButtonText = LocalizationService.Get("Dialog_Cancel"),
                DefaultButton = ContentDialogButton.Primary,
            };

            if (await ShowNestedDialogAsync(dialog) != ContentDialogResult.Primary) return;

            var newName = textBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(newName)) return;

            Vm.EditCategory(category, newName);
            RefreshList();
        }
        catch (Exception ex)
        {
            await ShowNestedErrorAsync(ex.Message);
        }
    }

    private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing) return;
        if (sender is not ComboBox { Tag: TestItem test } comboBox) return;

        var newStatus = comboBox.SelectedIndex switch
        {
            0 => TestItem.StatusTesting,
            1 => TestItem.StatusFix,
            2 => TestItem.StatusDone,
            _ => TestItem.StatusTesting
        };

        if (newStatus == test.Status) return;

        _isRefreshing = true;
        try { Vm.ChangeTestStatus(test, newStatus); }
        finally { _isRefreshing = false; }

        RefreshList();
    }

    private async void DeleteTest_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button { Tag: TestItem test }) return;

            var dialog = new ContentDialog
            {
                Title = LocalizationService.Get("DeleteConfirmTitle"),
                Content = LocalizationService.Get("DeleteConfirmMessage"),
                PrimaryButtonText = LocalizationService.Get("Dialog_Delete"),
                CloseButtonText = LocalizationService.Get("Dialog_Cancel"),
                DefaultButton = ContentDialogButton.Close,
            };

            if (await ShowNestedDialogAsync(dialog) != ContentDialogResult.Primary) return;

            Vm.DeleteTestCommand.Execute(test);
            RefreshList();
        }
        catch (Exception ex)
        {
            await ShowNestedErrorAsync(ex.Message);
        }
    }

    private async void EditTest_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button { Tag: TestItem test }) return;

            var textBox = new TextBox { Text = test.Text, MaxLength = 200 };

            var dialog = new ContentDialog
            {
                Title = LocalizationService.Get("TestEditTitle"),
                Content = textBox,
                PrimaryButtonText = LocalizationService.Get("Dialog_Save"),
                CloseButtonText = LocalizationService.Get("Dialog_Cancel"),
                DefaultButton = ContentDialogButton.Primary,
            };

            if (await ShowNestedDialogAsync(dialog) != ContentDialogResult.Primary) return;

            var newText = textBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(newText)) return;

            Vm.EditTest(test, newText);
            RefreshList();
        }
        catch (Exception ex)
        {
            await ShowNestedErrorAsync(ex.Message);
        }
    }

    private async void EditNote_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button { Tag: TestItem test }) return;
            await ShowNoteEditorAsync(test);
        }
        catch (Exception ex)
        {
            await ShowNestedErrorAsync(ex.Message);
        }
    }

    private async void AddNote_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not FrameworkElement { Tag: TestItem test }) return;
            await ShowNoteEditorAsync(test);
        }
        catch (Exception ex)
        {
            await ShowNestedErrorAsync(ex.Message);
        }
    }

    /// <summary>진행 내용 편집 다이얼로그를 표시합니다.</summary>
    private async Task ShowNoteEditorAsync(TestItem test)
    {
        var textBox = new TextBox
        {
            Text = test.ProgressNote,
            MaxLength = 500,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            Height = 120
        };

        var dialog = new ContentDialog
        {
            Title = LocalizationService.Get("TestEditNoteTitle"),
            Content = textBox,
            PrimaryButtonText = LocalizationService.Get("Dialog_Save"),
            CloseButtonText = LocalizationService.Get("Dialog_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
        };

        if (await ShowNestedDialogAsync(dialog) != ContentDialogResult.Primary) return;

        Vm.EditProgressNote(test, textBox.Text);
        RefreshList();
    }
}

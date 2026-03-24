using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>
/// 테스트 목록 다이얼로그 뷰모델.
/// 카테고리(TestCategory) 기반 중첩 구조로 테스트 항목을 관리합니다.
/// </summary>
public partial class TestListDialogViewModel : ObservableObject
{
    private readonly ProjectItem _projectItem;

    /// <summary>프로젝트 항목 참조</summary>
    public ProjectItem ProjectItem => _projectItem;

    /// <summary>새 카테고리 이름 입력</summary>
    [ObservableProperty]
    public partial string NewCategoryName { get; set; } = string.Empty;

    /// <summary>현재 탭 필터 ("All", "Testing", "Fix", "Done")</summary>
    [ObservableProperty]
    public partial string SelectedTab { get; set; } = "Testing";

    /// <summary>전체 카테고리 목록</summary>
    public ObservableCollection<TestCategory> Categories { get; }

    /// <summary>현재 탭 필터가 적용된 카테고리 목록 (UI 바인딩용)</summary>
    public ObservableCollection<TestCategory> FilteredCategories { get; } = [];

    public TestListDialogViewModel(ProjectItem projectItem)
    {
        _projectItem = projectItem;
        Categories = new ObservableCollection<TestCategory>(_projectItem.TestCategories ?? []);
        RefreshFilter();
    }

    partial void OnSelectedTabChanged(string value)
    {
        RefreshFilter();
    }

    /// <summary>탭 필터를 적용하여 FilteredCategories를 갱신합니다.</summary>
    public void RefreshFilter()
    {
        FilteredCategories.Clear();

        foreach (var cat in Categories)
        {
            var filteredItems = SelectedTab switch
            {
                "Testing" => cat.Items.Where(t => t.Status == TestItem.StatusTesting).ToList(),
                "Fix" => cat.Items.Where(t => t.Status == TestItem.StatusFix).ToList(),
                "Done" => cat.Items.Where(t => t.Status == TestItem.StatusDone).ToList(),
                _ => cat.Items.ToList()
            };

            // 필터 결과가 없는 카테고리는 테스트 탭에서만 표시 (항목 추가 가능)
            if (filteredItems.Count == 0 && SelectedTab != "Testing") continue;

            var filteredCat = new TestCategory
            {
                Id = cat.Id,
                Name = cat.Name,
                CreatedAt = cat.CreatedAt,
                Items = filteredItems
            };
            FilteredCategories.Add(filteredCat);
        }
    }

    [RelayCommand]
    private void ChangeTab(string tab)
    {
        SelectedTab = tab;
    }

    /// <summary>새 카테고리를 추가합니다.</summary>
    [RelayCommand]
    private void AddCategory()
    {
        var name = NewCategoryName?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        if (Categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return; // 중복 방지

        var newCat = new TestCategory { Name = name };
        Categories.Add(newCat);
        NewCategoryName = string.Empty;
        RefreshFilter();
    }

    /// <summary>카테고리를 삭제합니다 (소속 항목도 함께 삭제).</summary>
    [RelayCommand]
    private void DeleteCategory(TestCategory? category)
    {
        if (category is null) return;

        var original = Categories.FirstOrDefault(c => c.Id == category.Id);
        if (original is not null)
            Categories.Remove(original);

        RefreshFilter();
    }

    /// <summary>카테고리 이름을 수정합니다.</summary>
    public void EditCategory(TestCategory category, string newName)
    {
        ArgumentNullException.ThrowIfNull(category);
        var trimmed = newName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed)) return;

        var original = Categories.FirstOrDefault(c => c.Id == category.Id);
        if (original is not null)
            original.Name = trimmed;

        RefreshFilter();
    }

    /// <summary>특정 카테고리에 새 테스트 항목을 추가합니다.</summary>
    public void AddTestToCategory(TestCategory category, string text)
    {
        ArgumentNullException.ThrowIfNull(category);
        var trimmed = text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed)) return;

        var original = Categories.FirstOrDefault(c => c.Id == category.Id);
        if (original is null) return;

        // 같은 카테고리 내 중복 방지
        if (original.Items.Any(t => t.Text.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            return;

        var newTest = new TestItem { CategoryId = original.Id, Text = trimmed };
        original.Items.Add(newTest);
        RefreshFilter();
    }

    /// <summary>테스트 항목을 삭제합니다.</summary>
    [RelayCommand]
    private void DeleteTest(TestItem? test)
    {
        if (test is null) return;

        var cat = Categories.FirstOrDefault(c => c.Id == test.CategoryId);
        cat?.Items.Remove(test);
        RefreshFilter();
    }

    /// <summary>테스트 항목의 상태를 변경합니다.</summary>
    public void ChangeTestStatus(TestItem test, string newStatus)
    {
        ArgumentNullException.ThrowIfNull(test);

        var oldStatus = test.Status;
        test.Status = newStatus;

        if (newStatus == TestItem.StatusDone)
            test.CompletedAt = DateTime.Now;
        else if (oldStatus == TestItem.StatusDone)
            test.CompletedAt = null;

        if (SelectedTab != "All")
            RefreshFilter();
    }

    /// <summary>항목 텍스트를 수정합니다.</summary>
    public void EditTest(TestItem test, string newText)
    {
        ArgumentNullException.ThrowIfNull(test);
        var trimmed = newText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed)) return;
        test.Text = trimmed;
        RefreshFilter();
    }

    /// <summary>테스트 항목의 진행 내용을 수정합니다.</summary>
    public void EditProgressNote(TestItem test, string newNote)
    {
        ArgumentNullException.ThrowIfNull(test);
        test.ProgressNote = newNote?.Trim() ?? string.Empty;
    }

    /// <summary>카테고리 목록을 ProjectItem에 저장합니다.</summary>
    public void SaveToModel()
    {
        _projectItem.TestCategories = Categories.ToList();
    }
}

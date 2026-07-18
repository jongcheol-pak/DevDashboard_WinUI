using CommunityToolkit.Mvvm.ComponentModel;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>
/// 새 테스트/테스트 편집 다이얼로그 뷰모델 — 이름·스위트(카테고리)·방법을 편집합니다.
/// 편집 모드에서는 스위트 이동을 지원하지 않습니다(이름·방법만 수정 — 기존 관례).
/// </summary>
public partial class TestEditDialogViewModel : ObservableObject
{
    private readonly TestItem? _existing;

    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    [ObservableProperty] public partial string SelectedSuite { get; set; } = string.Empty;
    [ObservableProperty] public partial string Method { get; set; } = string.Empty;

    /// <summary>스위트 선택 목록 (기존 스위트 이름 — ComboBox 편집 가능으로 신규 입력도 허용)</summary>
    public IReadOnlyList<string> SuiteOptions { get; }

    /// <summary>편집 모드 여부 (false이면 새 테스트)</summary>
    public bool IsEditMode => _existing is not null;

    /// <summary>스위트 선택 가능 여부 — 새 테스트일 때만 (편집 시 스위트 이동 미지원)</summary>
    public bool CanEditSuite => !IsEditMode;

    public TestEditDialogViewModel(TestItem? existing, IReadOnlyList<string> suiteNames, string? presetSuite)
    {
        _existing = existing;
        SuiteOptions = suiteNames ?? [];

        var fallbackSuite = presetSuite ?? (SuiteOptions.Count > 0 ? SuiteOptions[0] : string.Empty);
        SelectedSuite = fallbackSuite;

        if (existing is not null)
        {
            Name = existing.Text;
            Method = existing.Method;
        }
    }

    /// <summary>입력값을 TestItem에 반영합니다. 편집이면 기존 항목을, 새 테스트면 새 항목을 반환합니다.
    /// 스위트 연결(CategoryId)은 호출부(TestPageViewModel)가 SelectedSuite로 처리합니다.</summary>
    public TestItem BuildResult()
    {
        var test = _existing ?? new TestItem();
        test.Text = Name.Trim();
        test.Method = Method?.Trim() ?? string.Empty;
        return test;
    }
}

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

    /// <summary>선택된 스위트. 목록에 없는 값이 들어오면 ComboBox가 선택을 비울 수 있어 null을 허용한다
    /// (빈 값은 저장 시 <see cref="TestEditDialog"/>가 차단한다).</summary>
    [ObservableProperty] public partial string? SelectedSuite { get; set; }

    [ObservableProperty] public partial string Method { get; set; } = string.Empty;

    /// <summary>스위트 선택 목록 = 작업 카테고리. 스위트 이름이 작업 카테고리와 같아야
    /// 칸반 카테고리 그룹의 통과율 배지(FR-T8)에 반영되므로 자유 입력을 두지 않는다.</summary>
    public IReadOnlyList<string> SuiteOptions { get; }

    /// <summary>편집 모드 여부 (false이면 새 테스트)</summary>
    public bool IsEditMode => _existing is not null;

    /// <summary>스위트 선택 가능 여부 — 새 테스트일 때만 (편집 시 스위트 이동 미지원)</summary>
    public bool CanEditSuite => !IsEditMode;

    /// <param name="taskCategories">스위트로 고를 수 있는 작업 카테고리 목록</param>
    /// <param name="presetSuite">기본 선택 스위트 (편집이면 그 테스트의 현재 스위트)</param>
    public TestEditDialogViewModel(TestItem? existing, IReadOnlyList<string> taskCategories, string? presetSuite)
    {
        _existing = existing;

        var options = (taskCategories ?? []).ToList();
        // 편집 대상의 현재 스위트가 작업 카테고리에 없으면(과거 자유 입력·"작업" 스위트) 목록에 넣는다.
        // 넣지 않으면 ComboBox가 선택을 비워 저장이 필수 검증에 막히고, 기존 스위트가 소실된다.
        if (!string.IsNullOrWhiteSpace(presetSuite)
            && !options.Contains(presetSuite, StringComparer.OrdinalIgnoreCase))
        {
            options.Add(presetSuite);
        }
        SuiteOptions = options;

        SelectedSuite = presetSuite ?? (SuiteOptions.Count > 0 ? SuiteOptions[0] : null);

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

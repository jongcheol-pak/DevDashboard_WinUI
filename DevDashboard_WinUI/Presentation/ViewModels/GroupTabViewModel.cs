using CommunityToolkit.Mvvm.ComponentModel;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>
/// 그룹 탭 한 개의 표시 상태.
/// <see cref="ProjectGroup"/>은 설정 JSON으로 저장되는 불변 record라 개수 같은 표시 값을 담을 수 없어,
/// 탭 표시용으로 감싸는 얇은 뷰모델을 둔다.
/// </summary>
public partial class GroupTabViewModel : ObservableObject
{
    /// <summary>탭이 가리키는 그룹</summary>
    public ProjectGroup Group { get; }

    /// <summary>이 그룹에 속한 프로젝트 수 (검색어가 있으면 검색 결과 기준)</summary>
    [ObservableProperty]
    public partial int Count { get; set; }

    /// <summary>
    /// 현재 선택된 탭인지 여부.
    /// 탭 목록이 다시 만들어져도 선택 표시가 유지되도록 값으로 들고 있습니다
    /// (RadioButton.IsChecked에 바인딩 — 컨테이너가 재생성돼도 복원됩니다).
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public GroupTabViewModel(ProjectGroup group) => Group = group;
}

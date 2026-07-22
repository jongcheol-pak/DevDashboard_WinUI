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

    public GroupTabViewModel(ProjectGroup group) => Group = group;
}

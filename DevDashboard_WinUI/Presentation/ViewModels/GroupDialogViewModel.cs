using CommunityToolkit.Mvvm.ComponentModel;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>그룹 추가/수정 팝업 뷰모델</summary>
public partial class GroupDialogViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>편집 중인 그룹 Id (null이면 신규 추가)</summary>
    public string? EditingGroupId { get; private set; }

    /// <summary>입력 값으로 ProjectGroup을 생성합니다.</summary>
    public ProjectGroup ToProjectGroup()
    {
        return new ProjectGroup
        {
            Id = EditingGroupId ?? Guid.NewGuid().ToString(),
            Name = Name.Trim()
        };
    }

    /// <summary>기존 그룹 데이터를 로드합니다 (수정 모드).</summary>
    public void LoadFrom(ProjectGroup group)
    {
        EditingGroupId = group.Id;
        Name = group.Name;
    }
}

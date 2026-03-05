using CommunityToolkit.Mvvm.ComponentModel;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>기술 스택 태그 뱃지 선택 항목</summary>
public partial class TagBadgeItem : ObservableObject
{
    /// <summary>태그 원본 이름</summary>
    public string Name { get; }

    /// <summary>뱃지 표시 이름 — 15자 초과 시 잘림</summary>
    public string DisplayName { get; }

    /// <summary>선택 여부</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public TagBadgeItem(string name)
    {
        Name = name;
        DisplayName = name.Length > 15 ? name[..15] : name;
    }
}

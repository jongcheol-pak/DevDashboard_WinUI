using CommunityToolkit.Mvvm.ComponentModel;
using DevDashboard.Infrastructure.Services;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>
/// 프로젝트 설정 다이얼로그의 카드 헤더 색상 견본 한 칸.
/// 태그 선택 뱃지(TagBadgeItem)와 같은 "선택 상태를 가진 목록 항목" 패턴입니다.
/// 색 문자열만 들고 있으며 브러시 변환은 View의 컨버터가 맡습니다.
/// </summary>
public partial class HeaderColorItem : ObservableObject
{
    /// <summary>색상 값 (#RRGGBB). 빈 문자열이면 "자동"(이름 해시로 배정) 칸입니다.</summary>
    public string Hex { get; }

    /// <summary>마우스를 올렸을 때 표시할 설명</summary>
    public string Tooltip => Hex.Length == 0 ? LocalizationService.Get("ProjHeaderColorAuto") : Hex;

    /// <summary>현재 선택된 칸인지 여부</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public HeaderColorItem(string hex) => Hex = hex;
}

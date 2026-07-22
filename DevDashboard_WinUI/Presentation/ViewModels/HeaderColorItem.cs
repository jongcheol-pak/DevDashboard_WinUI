using CommunityToolkit.Mvvm.ComponentModel;
using DevDashboard.Infrastructure.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>
/// 프로젝트 설정 다이얼로그의 카드 헤더 색상 견본 한 칸.
/// 태그 선택 뱃지(TagBadgeItem)와 같은 "선택 상태를 가진 목록 항목" 패턴입니다.
/// 견본 자체가 색이므로 브러시를 이 항목이 직접 들고 있습니다
/// (카드 화면의 색 변환은 컨버터가 맡습니다 — 여기는 고정 팔레트라 변환할 값이 없습니다).
/// </summary>
public partial class HeaderColorItem : ObservableObject
{
    /// <summary>선택된 칸의 테두리 — 시안의 2px #E8E6E3</summary>
    private static readonly SolidColorBrush SelectedBorder = new(Color.FromArgb(0xFF, 0xE8, 0xE6, 0xE3));

    /// <summary>"자동" 칸은 색이 없으므로 미선택 상태에서도 테두리로 자리를 보여준다</summary>
    private static readonly SolidColorBrush AutoBorder = new(Color.FromArgb(0xFF, 0x3A, 0x3A, 0x41));

    private static readonly SolidColorBrush NoBorder = new(Colors.Transparent);

    /// <summary>색상 값 (#RRGGBB). 빈 문자열이면 "자동"(이름 해시로 배정) 칸입니다.</summary>
    public string Hex { get; }

    /// <summary>"자동" 칸 여부 — 색 견본 대신 테두리만 그립니다.</summary>
    public bool IsAuto => Hex.Length == 0;

    /// <summary>견본 칸 배경 — "자동"은 투명</summary>
    public Brush SwatchBrush { get; }

    /// <summary>마우스를 올렸을 때 표시할 설명</summary>
    public string Tooltip => IsAuto ? LocalizationService.Get("ProjHeaderColorAuto") : Hex;

    /// <summary>현재 선택된 칸인지 여부</summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>칸 테두리 — 선택 표시</summary>
    public Brush BorderBrush => IsSelected ? SelectedBorder : IsAuto ? AutoBorder : NoBorder;

    partial void OnIsSelectedChanged(bool value) => OnPropertyChanged(nameof(BorderBrush));

    public HeaderColorItem(string hex)
    {
        Hex = hex;
        SwatchBrush = hex.Length == 0
            ? new SolidColorBrush(Colors.Transparent)
            : new SolidColorBrush(ParseHex(hex));
    }

    /// <summary>#RRGGBB 문자열을 Color로 변환합니다 (팔레트 상수만 들어오므로 형식은 항상 유효).</summary>
    private static Color ParseHex(string hex) => Color.FromArgb(
        0xFF,
        System.Convert.ToByte(hex.Substring(1, 2), 16),
        System.Convert.ToByte(hex.Substring(3, 2), 16),
        System.Convert.ToByte(hex.Substring(5, 2), 16));
}

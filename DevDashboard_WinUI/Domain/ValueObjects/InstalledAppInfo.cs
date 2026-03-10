using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevDashboard.Domain.ValueObjects;

/// <summary>
/// 현재 PC에 설치된 앱 정보 (설치 목록 다이얼로그 표시용).
/// </summary>
public sealed partial class InstalledAppInfo : ObservableObject
{
    /// <summary>앱 표시 이름</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>실행 파일 경로 (exe 또는 shell:AppsFolder 경로)</summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>AppUserModelId (UWP 앱용)</summary>
    public string? AppUserModelId { get; set; }

    /// <summary>추출된 아이콘 캐시 파일 경로</summary>
    public string? IconPath { get; set; }

    /// <summary>로드된 아이콘 이미지 (UI 바인딩용)</summary>
    [ObservableProperty]
    public partial BitmapImage? IconImage { get; set; }
}

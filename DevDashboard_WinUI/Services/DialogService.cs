using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Services;

/// <summary>
/// WinUI 3 ContentDialog 기반 다이얼로그 서비스.
/// App 시작 시 XamlRoot를 등록하고, ViewModel이 메시지 표시를 요청할 때 사용합니다.
/// </summary>
public static class DialogService
{
    private static XamlRoot? _xamlRoot;

    /// <summary>XamlRoot를 등록합니다. MainWindow.Activated 시 호출합니다.</summary>
    public static void SetXamlRoot(XamlRoot xamlRoot)
    {
        _xamlRoot = xamlRoot;
    }

    /// <summary>오류 메시지 다이얼로그를 표시합니다.</summary>
    public static async Task ShowErrorAsync(string message, string title = "오류")
    {
        if (_xamlRoot is null) return;

        var dialog = new ContentDialog
        {
            XamlRoot = _xamlRoot,
            Title = title,
            Content = message,
            CloseButtonText = "확인"
        };
        await dialog.ShowAsync();
    }

    /// <summary>확인/취소 다이얼로그를 표시합니다. 확인 시 true를 반환합니다.</summary>
    public static async Task<bool> ShowConfirmAsync(string message, string title = "확인")
    {
        if (_xamlRoot is null) return false;

        var dialog = new ContentDialog
        {
            XamlRoot = _xamlRoot,
            Title = title,
            Content = message,
            PrimaryButtonText = "예",
            CloseButtonText = "아니요"
        };
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}

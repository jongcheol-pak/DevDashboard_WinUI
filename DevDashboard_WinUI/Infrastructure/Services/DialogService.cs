using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Infrastructure.Services;

/// <summary>
/// 앱 전체에서 공통으로 사용하는 ContentDialog 기반 다이얼로그 서비스.
/// App.MainWindow의 XamlRoot를 사용합니다.
/// </summary>
public static class DialogService
{
    /// <summary>오류 메시지 다이얼로그를 표시합니다.</summary>
    public static async Task ShowErrorAsync(string message, string title = "오류")
    {
        var xamlRoot = App.MainWindow?.Content?.XamlRoot;
        if (xamlRoot is null) return;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "확인",
            XamlRoot = xamlRoot
        };
        await dialog.ShowAsync();
    }

    /// <summary>확인/취소 다이얼로그를 표시합니다. 확인 시 true를 반환합니다.</summary>
    public static async Task<bool> ShowConfirmAsync(string message, string title = "확인")
    {
        var xamlRoot = App.MainWindow?.Content?.XamlRoot;
        if (xamlRoot is null) return false;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "예",
            CloseButtonText = "아니요",
            XamlRoot = xamlRoot
        };
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}

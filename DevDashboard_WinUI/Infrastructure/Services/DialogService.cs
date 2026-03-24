using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;

namespace DevDashboard.Infrastructure.Services;

/// <summary>
/// 앱 전체에서 공통으로 사용하는 ContentDialog 기반 다이얼로그 서비스.
/// App.MainWindow의 XamlRoot를 사용합니다.
/// WinUI 3은 동일 XamlRoot에서 ContentDialog를 하나만 표시할 수 있으므로
/// SemaphoreSlim으로 동시 표시를 방지합니다.
/// </summary>
public static class DialogService
{
    /// <summary>동시 ContentDialog 표시 방지용 세마포어</summary>
    private static readonly SemaphoreSlim _dialogLock = new(1, 1);

    /// <summary>오류 메시지 다이얼로그를 표시합니다.</summary>
    public static async Task ShowErrorAsync(string message, string? title = null)
    {
        var xamlRoot = App.MainWindow?.Content?.XamlRoot;
        if (xamlRoot is null) return;

        if (!await _dialogLock.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            Debug.WriteLine($"[DialogService] 다이얼로그 표시 대기 타임아웃: {title}");
            return;
        }

        try
        {
            var dialog = new ContentDialog
            {
                Title = title ?? LocalizationService.Get("Dialog_DefaultErrorTitle"),
                Content = message,
                CloseButtonText = LocalizationService.Get("Dialog_OK"),
                XamlRoot = xamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DialogService] 다이얼로그 표시 실패: {ex.Message}");
        }
        finally
        {
            _dialogLock.Release();
        }
    }

    /// <summary>확인/취소 다이얼로그를 표시합니다. 확인 시 true를 반환합니다.</summary>
    public static async Task<bool> ShowConfirmAsync(string message, string? title = null)
    {
        var xamlRoot = App.MainWindow?.Content?.XamlRoot;
        if (xamlRoot is null) return false;

        if (!await _dialogLock.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            Debug.WriteLine($"[DialogService] 다이얼로그 표시 대기 타임아웃: {title}");
            return false;
        }

        try
        {
            var dialog = new ContentDialog
            {
                Title = title ?? LocalizationService.Get("Dialog_DefaultConfirmTitle"),
                Content = message,
                PrimaryButtonText = LocalizationService.Get("Dialog_Yes"),
                CloseButtonText = LocalizationService.Get("Dialog_No"),
                XamlRoot = xamlRoot
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DialogService] 다이얼로그 표시 실패: {ex.Message}");
            return false;
        }
        finally
        {
            _dialogLock.Release();
        }
    }
}

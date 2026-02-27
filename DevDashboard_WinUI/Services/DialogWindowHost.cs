using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using WinUIEx;

namespace DevDashboard.Services;

/// <summary>
/// Dialog Window를 소유자 창 위에, 태스크바 없이, 화면 중앙에 표시하는 헬퍼.
/// </summary>
internal static class DialogWindowHost
{
    // GWLP_HWNDPARENT: 소유자 창 핸들 설정 인덱스
    private const int GWLP_HWNDPARENT = -8;

    private static nint _ownerHwnd;

    /// <summary>부모 창을 등록합니다. 앱 시작 시 MainWindow에서 호출합니다.</summary>
    internal static void SetOwnerWindow(Window owner)
        => _ownerHwnd = WindowNative.GetWindowHandle(owner);

    /// <summary>
    /// Window를 소유자 창 위에, 태스크바 없이, 지정한 크기로 화면 중앙에 표시합니다.
    /// 모든 창 속성 설정 후 Activate()를 호출하여 첫 표시 시 올바른 크기/위치로 나타납니다.
    /// </summary>
    internal static void Show(Window dialog, int width = 800, int height = 600)
    {
        var hwnd = WindowNative.GetWindowHandle(dialog);

        // 소유자 창 설정 → 부모 창 위에 표시, 태스크바에 별도 항목 없음
        if (_ownerHwnd != 0)
            SetWindowLongPtr(hwnd, GWLP_HWNDPARENT, _ownerHwnd);

        // WS_EX_TOOLWINDOW: 태스크바 및 Alt+Tab 목록에서 제거
        HwndExtensions.ToggleExtendedWindowStyle(hwnd, true, ExtendedWindowStyle.ToolWindow);
        // WS_EX_APPWINDOW 제거: 태스크바 버튼 강제 표시 방지
        HwndExtensions.ToggleExtendedWindowStyle(hwnd, false, ExtendedWindowStyle.AppWindow);

        HwndExtensions.SetWindowSize(hwnd, width, height);
        HwndExtensions.CenterOnScreen(hwnd);

        // 소유자 창 비활성화 → 모달 동작 (다이얼로그가 열려 있는 동안 클릭 차단)
        if (_ownerHwnd != 0)
        {
            EnableWindow(_ownerHwnd, false);

            // 다이얼로그 닫힐 때 소유자 창 재활성화 후 포커스 복원
            void OnClosed(object? sender, WindowEventArgs e)
            {
                dialog.Closed -= OnClosed;
                EnableWindow(_ownerHwnd, true);
                SetForegroundWindow(_ownerHwnd);
            }
            dialog.Closed += OnClosed;
        }

        // 모든 창 속성 설정 후 활성화 → 첫 표시 시 올바른 크기/위치로 나타남
        dialog.Activate();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnableWindow(nint hWnd, bool bEnable);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);
}

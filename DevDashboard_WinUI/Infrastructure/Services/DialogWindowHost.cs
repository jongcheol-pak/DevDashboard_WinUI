using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;
using WinUIEx;

namespace DevDashboard.Infrastructure.Services;

/// <summary>
/// Dialog Window를 소유자 창 위에, 태스크바 없이, 화면 중앙에 표시하는 헬퍼.
/// 모달 입력 차단: XAML 오버레이(콘텐츠 영역) + WM_NCHITTEST 서브클래스(캡션 버튼/테두리).
/// EnableWindow(false)는 WM_ENABLE → VisualState 전이 + MicaBackdrop 비활성 전환으로
/// 깜빡임을 유발하므로 사용하지 않습니다.
/// </summary>
internal static class DialogWindowHost
{
    private const int GWLP_HWNDPARENT = -8;
    private const uint WM_NCHITTEST = 0x0084;
    private const nint HTCLIENT = 1;
    private const nuint SUBCLASS_ID = 1;
    private const int DWMWA_CLOAK = 13;

    private static nint _ownerHwnd;
    private static Panel? _contentRoot;

    /// <summary>모달 입력 차단 깊이 카운터 — 중첩 다이얼로그 대응</summary>
    private static int _modalDepth;
    private static Grid? _modalOverlay;

    /// <summary>SubclassProc 델리게이트를 정적 필드로 보관하여 GC 수집 방지</summary>
    private static readonly SubclassProc _subclassCallback = OwnerSubclassProc;

    /// <summary>부모 창을 등록합니다. 앱 시작 시 MainWindow에서 호출합니다.</summary>
    internal static void SetOwnerWindow(Window owner, Panel contentRoot)
    {
        _ownerHwnd = WindowNative.GetWindowHandle(owner);
        _contentRoot = contentRoot;
    }

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

        // 첫 렌더 전 검은 화면 방지: DWM 클로킹으로 창을 숨긴 채 활성화
        int cloaked = 1;
        DwmSetWindowAttribute(hwnd, DWMWA_CLOAK, ref cloaked, sizeof(int));

        // 소유자 창 모달 입력 차단 시작
        PushModal();

        void OnClosed(object? sender, WindowEventArgs e)
        {
            dialog.Closed -= OnClosed;
            PopModal();
            if (_ownerHwnd != 0)
                SetForegroundWindow(_ownerHwnd);
        }
        dialog.Closed += OnClosed;

        // 창 활성화 → XAML 렌더링 시작 (창은 클로킹 상태라 화면에 보이지 않음)
        dialog.Activate();

        // 낮은 우선순위 큐: XAML 첫 렌더 패스 완료 후 클로킹 해제 → 렌더된 상태로 표시
        dialog.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            int uncloaked = 0;
            DwmSetWindowAttribute(hwnd, DWMWA_CLOAK, ref uncloaked, sizeof(int));
        });
    }

    /// <summary>소유자 창에 모달 입력 차단을 적용합니다.</summary>
    private static void PushModal()
    {
        _modalDepth++;
        if (_modalDepth > 1) return; // 이미 모달 활성 상태

        // 1. XAML 오버레이: 콘텐츠 영역 포인터 입력 차단
        if (_contentRoot is not null)
        {
            _modalOverlay = new Grid
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0x40, 0, 0, 0))
            };
            // RootGrid 전체를 덮도록 RowSpan/ColumnSpan 설정
            Grid.SetRowSpan(_modalOverlay, 100);
            Grid.SetColumnSpan(_modalOverlay, 100);
            _contentRoot.Children.Add(_modalOverlay);
        }

        // 2. Win32 서브클래스: 캡션 버튼/테두리 등 비클라이언트 영역 입력 차단
        if (_ownerHwnd != 0)
            SetWindowSubclass(_ownerHwnd, _subclassCallback, SUBCLASS_ID, 0);
    }

    /// <summary>소유자 창의 모달 입력 차단을 해제합니다.</summary>
    private static void PopModal()
    {
        _modalDepth = Math.Max(0, _modalDepth - 1);
        if (_modalDepth > 0) return; // 아직 다른 다이얼로그가 열려 있음

        // XAML 오버레이 제거
        if (_modalOverlay is not null && _contentRoot is not null)
        {
            _contentRoot.Children.Remove(_modalOverlay);
            _modalOverlay = null;
        }

        // Win32 서브클래스 제거
        if (_ownerHwnd != 0)
            RemoveWindowSubclass(_ownerHwnd, _subclassCallback, SUBCLASS_ID);
    }

    /// <summary>
    /// 모달 상태에서 비클라이언트 영역(캡션 버튼, 테두리 등)의 히트 테스트를
    /// HTCLIENT로 변환하여 XAML 오버레이가 차단하도록 합니다.
    /// </summary>
    private static nint OwnerSubclassProc(
        nint hWnd, uint uMsg, nint wParam, nint lParam,
        nuint uIdSubclass, nint dwRefData)
    {
        if (_modalDepth > 0 && uMsg == WM_NCHITTEST)
        {
            var result = DefSubclassProc(hWnd, uMsg, wParam, lParam);
            // 비클라이언트 영역 → HTCLIENT로 변환하여 XAML 오버레이가 포인터 이벤트를 흡수
            if (result != HTCLIENT)
                return HTCLIENT;
        }

        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    // ===== P/Invoke 선언 =====

    private delegate nint SubclassProc(
        nint hWnd, uint uMsg, nint wParam, nint lParam,
        nuint uIdSubclass, nint dwRefData);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("comctl32.dll")]
    private static extern bool SetWindowSubclass(
        nint hWnd, SubclassProc pfnSubclass, nuint uIdSubclass, nint dwRefData);

    [DllImport("comctl32.dll")]
    private static extern bool RemoveWindowSubclass(
        nint hWnd, SubclassProc pfnSubclass, nuint uIdSubclass);

    [DllImport("comctl32.dll")]
    private static extern nint DefSubclassProc(nint hWnd, uint uMsg, nint wParam, nint lParam);
}

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace DevDashboard;

/// <summary>단일 인스턴스 진입점 — Mutex로 중복 실행을 검사하고, 이미 실행 중이면 기존 창을 활성화한 뒤 종료합니다.</summary>
partial class Program
{
    /// <summary>프로세스 수명 동안 유지 — GC 수집 방지용</summary>
    private static Mutex? _singleInstanceMutex;

    [STAThread]
    static void Main(string[] args)
    {
        // COM 래퍼 초기화 — 모든 WinRT/COM 호출보다 반드시 먼저 실행해야 한다.
        WinRT.ComWrappersSupport.InitializeComWrappers();

        // Mutex로 단일 인스턴스 검사 (COM 의존 없음, 부팅 시 자동 실행에서도 안전)
        _singleInstanceMutex = new Mutex(true, @"Global\DevDashboard_WinUI_SingleInstance", out var createdNew);
        if (!createdNew)
        {
            // 이미 실행 중 — 기존 인스턴스로 활성화 리디렉션 시도 후 종료
            try
            {
                var mainInstance = AppInstance.FindOrRegisterForKey("DevDashboard_SingleInstance");
                if (!mainInstance.IsCurrent)
                {
                    var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                    mainInstance.RedirectActivationToAsync(activatedArgs).AsTask().Wait();
                }
            }
            catch
            {
                // 리디렉션 실패 시 무시하고 종료 (기존 인스턴스가 이미 존재하므로 문제 없음)
            }

            _singleInstanceMutex.Dispose();
            return;
        }

        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });

    }
}

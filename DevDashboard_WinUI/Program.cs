using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace DevDashboard;

/// <summary>단일 인스턴스 진입점 — 이미 실행 중인 인스턴스가 있으면 해당 창을 활성화하고 종료합니다.</summary>
internal static class Program
{
    [STAThread]
    private static async Task<int> Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        var isRedirect = await RedirectIfNotFirstInstanceAsync();
        if (isRedirect)
            return 0;

        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);

            _ = new App();
        });

        return 0;
    }

    /// <summary>첫 번째 인스턴스가 아니면 기존 인스턴스로 활성화를 리디렉션합니다.</summary>
    /// <returns>리디렉션되었으면 <c>true</c></returns>
    private static async Task<bool> RedirectIfNotFirstInstanceAsync()
    {
        var mainInstance = AppInstance.FindOrRegisterForKey("DevDashboard_SingleInstance");

        if (mainInstance.IsCurrent)
        {
            // 첫 번째 인스턴스 — 이후 실행 요청 수신 시 기존 창 활성화
            mainInstance.Activated += OnActivated;
            return false;
        }

        // 이미 실행 중 — 기존 인스턴스로 활성화 리디렉션 후 종료
        var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        await mainInstance.RedirectActivationToAsync(activatedArgs);
        return true;
    }

    /// <summary>다른 인스턴스가 실행되었을 때 기존 창을 포그라운드로 활성화합니다.</summary>
    private static void OnActivated(object? sender, AppActivationArguments args)
    {
        App.BringMainWindowToForeground();
    }
}

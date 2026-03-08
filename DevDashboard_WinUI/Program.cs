using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace DevDashboard;

/// <summary>단일 인스턴스 진입점 — Mutex로 중복 실행을 검사하고, 이미 실행 중이면 기존 창을 활성화한 뒤 종료합니다.</summary>
partial class Program
{
    /// <summary>프로세스 수명 동안 유지 — GC 수집 방지용</summary>
    private static Mutex? _singleInstanceMutex;

    private static void Main(string[] args)
    {
        WriteCrashLog("1");
        // XAML 프레임워크 내부 크래시(FailFast/STATUS_STOWED_EXCEPTION)도 잡기 위해
        // Application.Start()보다 먼저 저수준 예외 핸들러를 등록한다.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            WriteCrashLog($"[AppDomain.UnhandledException] IsTerminating={e.IsTerminating}\n{e.ExceptionObject}");
        };

        try
        {
            WriteCrashLog("2");
            // COM 래퍼 초기화 — 모든 WinRT/COM 호출보다 먼저 실행해야 한다.
            WinRT.ComWrappersSupport.InitializeComWrappers();

            // Mutex로 단일 인스턴스 검사 (COM 의존 없음, 부팅 시 자동 실행에서도 안전)
            _singleInstanceMutex = new Mutex(true, @"Global\DevDashboard_WinUI_SingleInstance", out var createdNew);
            WriteCrashLog("3");
            if (!createdNew)
            {
                // 이미 실행 중 — 기존 인스턴스로 활성화 리디렉션 시도 후 종료
                try
                {
                    WriteCrashLog("4");
                    var mainInstance = AppInstance.FindOrRegisterForKey("DevDashboard_SingleInstance");
                    if (!mainInstance.IsCurrent)
                    {
                        WriteCrashLog("5");
                        var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
                        mainInstance.RedirectActivationToAsync(activatedArgs).AsTask().Wait();
                    }
                }
                catch
                {
                    WriteCrashLog("6");
                    // 리디렉션 실패 시 무시하고 종료 (기존 인스턴스가 이미 존재하므로 문제 없음)
                }

                WriteCrashLog("7");
                _singleInstanceMutex.Dispose();
                return;
            }

            WriteCrashLog("8");
            WaitForGraphicsReady();

            WriteCrashLog("8.5");
            Application.Start(p =>
            {
                WriteCrashLog("9");
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);

                _ = new App();

                WriteCrashLog("10");
            });

            WriteCrashLog("11");
        }
        catch (Exception ex)
        {
            WriteCrashLog($"[Main] {ex}");
        }
    }

    /// <summary>DXGI, DWM, Shell이 모두 준비될 때까지 대기한다. 부팅 직후 Application.Start() 크래시를 방지한다.</summary>
    private static void WaitForGraphicsReady()
    {
        const int maxWaitMs = 90_000;
        const int pollIntervalMs = 1_000;
        var waited = 0;

        while (waited < maxWaitMs)
        {
            var dxgi = IsDxgiReady();
            var dwm = IsDwmReady();
            var shell = IsShellReady();

            if (dxgi && dwm && shell)
            {
                WriteCrashLog($"[Graphics] All ready (waited {waited}ms)");
                return;
            }

            WriteCrashLog($"[Graphics] DXGI={dxgi} DWM={dwm} Shell={shell}, waiting... ({waited}ms)");
            Thread.Sleep(pollIntervalMs);
            waited += pollIntervalMs;
        }

        WriteCrashLog($"[Graphics] Timeout after {maxWaitMs}ms, proceeding anyway");
    }

    private static bool IsDxgiReady()
    {
        try
        {
            var hr = CreateDXGIFactory1(typeof(IDXGIFactory1).GUID, out var factory);
            if (hr >= 0 && factory != nint.Zero)
            {
                Marshal.Release(factory);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsDwmReady()
    {
        try
        {
            var hr = DwmIsCompositionEnabled(out var enabled);
            return hr >= 0 && enabled;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsShellReady()
    {
        return GetShellWindow() != nint.Zero;
    }

    [DllImport("dxgi.dll", PreserveSig = true)]
    private static extern int CreateDXGIFactory1(in Guid riid, out nint ppFactory);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmIsCompositionEnabled(out bool enabled);

    [DllImport("user32.dll")]
    private static extern nint GetShellWindow();

    [Guid("770aae78-f26f-4dba-a829-253c83d1b387")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDXGIFactory1 { }

    /// <summary>크래시 로그를 기록한다. 패키지 인프라 미준비 시 TEMP 폴더에 폴백한다.</summary>
    internal static void WriteCrashLog(string message)
    {
        var entry = $"{DateTime.Now}\nUptime={Environment.TickCount64}ms\n{message}\n\n";

        try
        {
            // 1차: MSIX LocalFolder
            var logPath = Path.Combine(
                Windows.Storage.ApplicationData.Current.LocalFolder.Path,
                "crash.log");
            File.AppendAllText(logPath, entry);
        }
        catch
        {
            try
            {
                // 2차: TEMP 폴더 (부팅 직후 패키지 인프라 미초기화 시 폴백)
                var logPath = Path.Combine(Path.GetTempPath(), "DevDashboard_crash.log");
                File.AppendAllText(logPath, entry);
            }
            catch
            {
                // 로그 기록 자체가 실패하면 무시
            }
        }
    }
}

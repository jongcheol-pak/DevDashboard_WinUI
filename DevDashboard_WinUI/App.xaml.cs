using System.Globalization;
using System.Runtime.InteropServices;
using DevDashboard.Infrastructure.Persistence;
using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace DevDashboard;

public partial class App : Application
{
    /// <summary>메인 창 참조 — FileOpenPicker/FolderPicker hwnd 획득 등에 사용</summary>
    public static Window? MainWindow { get; private set; }

    private static DispatcherQueue? _dispatcherQueue;

    public App()
    {
        Program.WriteCrashLog("12");
        InitializeComponent();
        Program.WriteCrashLog("13");
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Program.WriteCrashLog("14");
        UnhandledException += OnUnhandledException;
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Program.WriteCrashLog($"[UnhandledException] {e.Exception}");
        e.Handled = false;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            Program.WriteCrashLog("[OnLaunched] Step 1: RegisterSingleInstanceActivation");
            RegisterSingleInstanceActivation();

            Program.WriteCrashLog("[OnLaunched] Step 2: JsonStorageService.Load");
            var storageService = new JsonStorageService();
            var settings = storageService.Load();

            Program.WriteCrashLog("[OnLaunched] Step 3: ApplyLanguageSetting");
            ApplyLanguageSetting(settings.Language);

            Program.WriteCrashLog("[OnLaunched] Step 4: DatabaseContext");
            SqliteProjectRepository? projectRepository = null;
            string? dbErrorMessage = null;
            try
            {
                var dbContext = new DatabaseContext();
                projectRepository = new SqliteProjectRepository(dbContext);
            }
            catch (Exception ex)
            {
                dbErrorMessage = ex.Message;
            }

            Program.WriteCrashLog("[OnLaunched] Step 5: new MainWindow");
            var mainWindow = new MainWindow(settings, storageService, projectRepository, dbErrorMessage);
            MainWindow = mainWindow;

            Program.WriteCrashLog("[OnLaunched] Step 6: ApplyTheme");
            AppSettingsDialogViewModel.ApplyTheme(settings.ThemeMode);

            Program.WriteCrashLog("[OnLaunched] Step 7: Activate");
            mainWindow.Activate();

            Program.WriteCrashLog("[OnLaunched] Step 8: Done");
        }
        catch (Exception ex)
        {
            Program.WriteCrashLog($"[OnLaunched] {ex}");
            throw;
        }
    }

    /// <summary>언어 설정에 따라 PrimaryLanguageOverride를 지정합니다.
    /// ResourceLoader(.resw)가 올바른 언어 파일을 로드하도록 합니다.</summary>
    internal static void ApplyLanguageSetting(LanguageSetting lang)
    {
        var useEnglish = lang == LanguageSetting.English ||
            (lang == LanguageSetting.SystemDefault &&
             !CultureInfo.CurrentUICulture.Name.StartsWith("ko", StringComparison.OrdinalIgnoreCase));

        Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride =
            useEnglish ? "en-US" : "ko-KR";
    }

    /// <summary>다른 인스턴스 실행 시 기존 메인 창을 포그라운드로 활성화합니다.</summary>
    internal static void BringMainWindowToForeground()
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            if (MainWindow is null) return;

            MainWindow.Activate();

            // 최소화 상태 복원 및 포그라운드 전환
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
            if (IsIconic(hwnd))
                ShowWindow(hwnd, SW_RESTORE);
            SetForegroundWindow(hwnd);
        });
    }

    /// <summary>AppInstance에 단일 인스턴스 키를 등록하여 다른 인스턴스의 활성화 요청을 수신합니다.</summary>
    private static void RegisterSingleInstanceActivation()
    {
        try
        {
            var appInstance = AppInstance.FindOrRegisterForKey("DevDashboard_SingleInstance");
            appInstance.Activated += (_, _) => BringMainWindowToForeground();
        }
        catch
        {
            // 부팅 직후 COM 서브시스템이 미초기화 상태일 수 있음 — 무시 (Mutex가 단일 인스턴스를 보장)
        }
    }

    private const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(nint hWnd);
}

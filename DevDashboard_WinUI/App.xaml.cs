using System.Globalization;
using System.Runtime.InteropServices;
using DevDashboard.Infrastructure.Persistence;
using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace DevDashboard;

public partial class App : Application
{
    /// <summary>메인 창 참조 — FileOpenPicker/FolderPicker hwnd 획득 등에 사용</summary>
    public static Window? MainWindow { get; private set; }

    private static DispatcherQueue? _dispatcherQueue;

    public App()
    {
        InitializeComponent();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var storageService = new JsonStorageService();
        var settings = storageService.Load();

        // 언어 설정 적용 — ResourceLoader 생성 전에 호출해야 함
        ApplyLanguageSetting(settings.Language);

        // SQLite 프로젝트 저장소 초기화
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

        var mainWindow = new MainWindow(settings, storageService, projectRepository, dbErrorMessage);
        MainWindow = mainWindow;

        // 테마 적용 — MainWindow.Content가 생성된 후 호출해야 root 요소에 RequestedTheme 설정 가능
        AppSettingsDialogViewModel.ApplyTheme(settings.ThemeMode);

        mainWindow.Activate();
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

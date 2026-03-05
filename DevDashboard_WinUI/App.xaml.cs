using System.Globalization;
using DevDashboard.Infrastructure.Persistence;
using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;

namespace DevDashboard;

public partial class App : Application
{
    /// <summary>메인 창 참조 — FileOpenPicker/FolderPicker hwnd 획득 등에 사용</summary>
    public static Window? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
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
}

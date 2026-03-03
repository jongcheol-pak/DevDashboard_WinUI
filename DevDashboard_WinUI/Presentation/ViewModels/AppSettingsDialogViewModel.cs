using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevDashboard.Infrastructure.Services;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>태그/카테고리 표시 항목 (기본값 여부 포함)</summary>
/// <param name="Value">표시할 문자열 값</param>
/// <param name="IsDefault">기본 제공 항목 여부 (true면 삭제 버튼 숨김)</param>
public record TagDisplayItem(string Value, bool IsDefault);

/// <summary>앱 설정 팝업 뷰모델</summary>
public partial class AppSettingsDialogViewModel : ObservableObject
{
    private const string StartupTaskId = "DevDashboardStartup";

    public AppSettingsDialogViewModel()
    {
        Tools.CollectionChanged += OnToolsCollectionChanged;
        TechStackTags.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CanAddTechTag));
            OnPropertyChanged(nameof(AllTechStackTags));
        };
        Categories.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CanAddCategory));
            OnPropertyChanged(nameof(AllCategories));
        };
    }

    // --- 좌측 메뉴 ---

    /// <summary>선택된 메뉴 인덱스 (0: 설정, 1: 도구, 2: 코드, 3: 정보)</summary>
    [ObservableProperty]
    private int _selectedMenuIndex;

    // --- 정보 탭 ---

    /// <summary>앱 버전 (레지스트리에서 읽어옴)</summary>
    public string AppVersion { get; } = LocalizationService.Get("VersionPrefix") + VersionCheckService.ReadCurrentVersion();

    /// <summary>최신 버전 문자열</summary>
    [ObservableProperty]
    private string _latestVersionText = string.Empty;

    /// <summary>최신 버전 다운로드 URL</summary>
    [ObservableProperty]
    private string _latestReleaseUrl = string.Empty;

    /// <summary>최신 버전이 존재하는지 여부</summary>
    [ObservableProperty]
    private bool _hasNewVersion;

    /// <summary>MS Store를 통해 최신 버전을 확인합니다 (세션 내 캐시 적용).</summary>
    public async Task CheckLatestVersionAsync()
    {
        var result = await VersionCheckService.CheckLatestVersionAsync();
        if (result is not null)
        {
            LatestVersionText = LocalizationService.Get("LatestVersionPrefix") + result.VersionText;
            LatestReleaseUrl = result.ReleaseUrl;
            HasNewVersion = true;
        }
    }

    /// <summary>MS Store 업데이트 페이지를 엽니다.</summary>
    [RelayCommand]
    private void OpenLatestRelease() => VersionCheckService.OpenUrl(LatestReleaseUrl);

    /// <summary>사용 중인 오픈소스 라이브러리 목록</summary>
    public OpenSourceItem[] OpenSourceLibraries { get; } =
    [
        new("Microsoft.WindowsAppSDK", "MIT License", "https://github.com/microsoft/WindowsAppSDK"),
        new("WinUIEx", "MIT License", "https://github.com/dotMorten/WinUIEx"),
        new("CommunityToolkit.Mvvm", "MIT License", "https://github.com/CommunityToolkit/dotnet"),
        new("CommunityToolkit.WinUI", "MIT License", "https://github.com/CommunityToolkit/Windows"),
        new("Microsoft.Data.Sqlite", "MIT License", "https://github.com/dotnet/efcore"),
    ];

    /// <summary>오픈소스 사이트 URL을 기본 브라우저로 엽니다.</summary>
    [RelayCommand]
    private void OpenLicenseUrl(string? url) => VersionCheckService.OpenUrl(url ?? string.Empty);

    // --- 설정 탭 ---

    [ObservableProperty]
    private bool _runOnStartup;

    /// <summary>진행 중인 To-Do 완료 시 작업 기록 팝업 표시 여부</summary>
    [ObservableProperty]
    private bool _showWorkLogPopupOnTodoComplete;

    /// <summary>태그 마키 애니메이션 활성화 여부</summary>
    [ObservableProperty]
    private bool _enableTagAnimation = true;

    /// <summary>ComboBox 바인딩용 언어 선택 항목 목록</summary>
    public LanguageItem[] LanguageItems { get; } =
    [
        new(LanguageSetting.SystemDefault, LocalizationService.Get("Lang_System")),
        new(LanguageSetting.Korean, LocalizationService.Get("Lang_Korean")),
        new(LanguageSetting.English, LocalizationService.Get("Lang_English"))
    ];

    /// <summary>ComboBox에서 선택된 언어 항목</summary>
    [ObservableProperty]
    private LanguageItem? _selectedLanguageItem;

    /// <summary>ComboBox 바인딩용 테마 선택 항목 목록</summary>
    public ThemeModeItem[] ThemeModeItems { get; } =
    [
        new(ThemeMode.Light, LocalizationService.Get("Theme_Light")),
        new(ThemeMode.Dark, LocalizationService.Get("Theme_Dark")),
        new(ThemeMode.System, LocalizationService.Get("Theme_System"))
    ];

    /// <summary>ComboBox에서 선택된 테마 항목</summary>
    [ObservableProperty]
    private ThemeModeItem? _selectedThemeModeItem;

    /// <summary>테마 변경 시 즉시 적용</summary>
    partial void OnSelectedThemeModeItemChanged(ThemeModeItem? value)
    {
        if (value is not null)
            ApplyTheme(value.Value);
    }

    // --- 도구 탭 ---

    /// <summary>사용자 정의 외부 도구 목록</summary>
    public ObservableCollection<ToolItemViewModel> Tools { get; } = [];

    private void OnToolsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(Tools));
    }

    // --- 코드 탭 ---

    /// <summary>기본 제공 기술 스택 태그 목록</summary>
    public static IReadOnlyList<string> DefaultTechStackTags { get; } =
    [
        "node.js", "javascript", "typescript", "java", "python", "swift", "kotlin",
        "react", "js", "php", "ruby", "c#", "c", "c++", "objective-c", "flutter", "ai",
        ".net", ".net framework", "html", "winui", "rust", "asp.net","wpf"
    ];

    /// <summary>기술 스택 태그 목록</summary>
    public ObservableCollection<string> TechStackTags { get; } = [];

    /// <summary>기본 항목(앞) + 사용자 항목(뒤) 합산 표시 목록</summary>
    public IEnumerable<TagDisplayItem> AllTechStackTags =>
        DefaultTechStackTags.Select(t => new TagDisplayItem(t, true))
            .Concat(TechStackTags.Select(t => new TagDisplayItem(t, false)));

    /// <summary>기술 스택 태그 등록 가능 여부</summary>
    public bool CanAddTechTag => DefaultTechStackTags.Count + TechStackTags.Count < 50;

    /// <summary>카테고리 등록 가능 여부</summary>
    public bool CanAddCategory => DefaultCategories.Count + Categories.Count < 50;

    /// <summary>인라인 입력 — 새 태그 텍스트</summary>
    [ObservableProperty]
    private string _newTagText = string.Empty;

    /// <summary>NewTagText 값을 TechStackTags에 추가합니다.</summary>
    [RelayCommand]
    private void AddTechTag()
    {
        var tag = NewTagText.Trim();
        if (string.IsNullOrWhiteSpace(tag)) return;
        if (TechStackTags.Contains(tag, StringComparer.OrdinalIgnoreCase)) return;
        if (!CanAddTechTag) return;
        TechStackTags.Add(tag);
        NewTagText = string.Empty;
    }

    /// <summary>지정된 태그를 목록에서 제거합니다.</summary>
    [RelayCommand]
    private void RemoveTechTag(string tag) => TechStackTags.Remove(tag);

    /// <summary>카테고리 목록</summary>
    public ObservableCollection<string> Categories { get; } = [];

    /// <summary>기본 항목(앞) + 사용자 항목(뒤) 합산 표시 목록</summary>
    public IEnumerable<TagDisplayItem> AllCategories =>
        DefaultCategories.Select(t => new TagDisplayItem(t, true))
            .Concat(Categories.Select(t => new TagDisplayItem(t, false)));

    /// <summary>기본 제공 카테고리 목록</summary>
    public static IReadOnlyList<string> DefaultCategories { get; } =
    [
        "AI", "Game", "Mobile", "Web", "Windows"
    ];

    /// <summary>인라인 입력 — 새 카테고리 텍스트</summary>
    [ObservableProperty]
    private string _newCategoryText = string.Empty;

    /// <summary>NewCategoryText 값을 Categories에 추가합니다.</summary>
    [RelayCommand]
    private void AddCategory()
    {
        var cat = NewCategoryText.Trim();
        if (string.IsNullOrWhiteSpace(cat)) return;
        if (Categories.Contains(cat, StringComparer.OrdinalIgnoreCase)) return;
        if (!CanAddCategory) return;
        Categories.Add(cat);
        NewCategoryText = string.Empty;
    }

    /// <summary>지정된 카테고리를 목록에서 제거합니다.</summary>
    [RelayCommand]
    private void RemoveCategory(string category) => Categories.Remove(category);

    /// <summary>도구 추가</summary>
    [RelayCommand]
    private void AddTool()
    {
        if (Tools.Any(t => t.IsNew))
            return;

        var item = CreateToolItem();
        item.MarkAsNew();
        item.IsEditing = true;
        item.EditName = string.Empty;
        item.EditExecutablePath = string.Empty;
        Tools.Add(item);

        ExpandOnly(item);
    }

    private void ExpandOnly(ToolItemViewModel target)
    {
        foreach (var t in Tools)
            t.IsExpanded = ReferenceEquals(t, target);
    }

    private bool IsDuplicateToolName(ToolItemViewModel self, string name)
    {
        return Tools.Any(t => !ReferenceEquals(t, self)
            && t.Name.Trim().Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private ToolItemViewModel CreateToolItem()
    {
        return new ToolItemViewModel(
            onRequestExpand: ExpandOnly,
            onRequestDelete: item => Tools.Remove(item),
            onCheckDuplicateName: IsDuplicateToolName);
    }

    // --- 로드 / 적용 ---

    /// <summary>현재 AppSettings 값을 로드합니다.</summary>
    public void LoadFrom(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        ShowWorkLogPopupOnTodoComplete = settings.ShowWorkLogPopupOnTodoComplete;
        EnableTagAnimation = settings.EnableTagAnimation;
        SelectedLanguageItem = LanguageItems.FirstOrDefault(l => l.Value == settings.Language)
            ?? LanguageItems[0];
        SelectedThemeModeItem = ThemeModeItems.FirstOrDefault(t => t.Value == settings.ThemeMode)
            ?? ThemeModeItems[0];

        Tools.Clear();
        foreach (var tool in settings.Tools)
        {
            var item = CreateToolItem();
            item.LoadFrom(tool);
            Tools.Add(item);
        }

        TechStackTags.Clear();
        foreach (var tag in settings.TechStackTags)
            TechStackTags.Add(tag);

        Categories.Clear();
        foreach (var category in settings.Categories)
            Categories.Add(category);
    }

    /// <summary>변경 값을 AppSettings에 반영하고 시스템에 적용합니다.</summary>
    public void ApplyTo(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.ShowWorkLogPopupOnTodoComplete = ShowWorkLogPopupOnTodoComplete;
        settings.EnableTagAnimation = EnableTagAnimation;
        settings.Language = SelectedLanguageItem?.Value ?? LanguageSetting.SystemDefault;
        settings.ThemeMode = SelectedThemeModeItem?.Value ?? ThemeMode.Light;

        settings.Tools.Clear();
        foreach (var item in Tools.Where(t => !t.IsNew))
            settings.Tools.Add(item.ToModel());

        settings.TechStackTags.Clear();
        settings.TechStackTags.AddRange(TechStackTags);

        settings.Categories.Clear();
        settings.Categories.AddRange(Categories);

        ApplyTheme(SelectedThemeModeItem?.Value ?? ThemeMode.Light);
    }

    /// <summary>테마를 즉시 적용합니다 (WinUI 3 ElementTheme 방식).</summary>
    public static void ApplyTheme(ThemeMode mode)
    {
        var theme = mode switch
        {
            ThemeMode.Dark => ElementTheme.Dark,
            ThemeMode.Light => ElementTheme.Light,
            _ => ElementTheme.Default // System
        };

        if (App.MainWindow?.Content is FrameworkElement root)
            root.RequestedTheme = theme;
    }

    /// <summary>ThemeMode를 ElementTheme으로 변환합니다.</summary>
    public static ElementTheme ToElementTheme(ThemeMode mode) => mode switch
    {
        ThemeMode.Dark => ElementTheme.Dark,
        ThemeMode.Light => ElementTheme.Light,
        _ => ElementTheme.Default
    };

    /// <summary>StartupTask API를 통해 자동 실행 상태를 비동기로 읽습니다.</summary>
    public async Task LoadStartupStateAsync()
    {
        try
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            RunOnStartup = task.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
        }
        catch (Exception)
        {
            RunOnStartup = false;
        }
    }

    /// <summary>StartupTask API를 통해 자동 실행 상태를 설정합니다.</summary>
    public async Task ApplyStartupTaskAsync(bool enable)
    {
        try
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            if (enable)
                await task.RequestEnableAsync();
            else
                task.Disable();
        }
        catch (Exception)
        {
            // StartupTask 접근 실패 — 조용히 처리
        }
    }
}

/// <summary>ComboBox 표시용 테마 항목</summary>
public record ThemeModeItem(ThemeMode Value, string DisplayName);

/// <summary>ComboBox 표시용 언어 항목</summary>
public record LanguageItem(LanguageSetting Value, string DisplayName);

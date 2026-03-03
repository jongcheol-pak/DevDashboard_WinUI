using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevDashboard.Infrastructure.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>프로젝트 추가/수정 팝업 뷰모델</summary>
public partial class ProjectSettingsDialogViewModel : ObservableObject
{
    /// <summary>셸 도구 이름 상수 — PowerShell</summary>
    public const string PowerShellToolName = "PowerShell";

    /// <summary>셸 도구 이름 상수 — 명령 프롬프트</summary>
    public const string CmdToolName = "CMD";

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>프로젝트 아이콘 이미지 파일 경로</summary>
    [ObservableProperty]
    private string _iconPath = string.Empty;

    [ObservableProperty]
    private string _path = string.Empty;

    [ObservableProperty]
    private string _options = string.Empty;

    [ObservableProperty]
    private string _command = string.Empty;

    [ObservableProperty]
    private bool _useWorkingDirectory;

    [ObservableProperty]
    private string _shellWorkingDirectory = string.Empty;

    [ObservableProperty]
    private ExternalTool? _selectedDevTool;

    [ObservableProperty]
    private bool _isShellTool;

    [ObservableProperty]
    private bool _runAsAdmin;

    [ObservableProperty]
    private string _tags = string.Empty;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private string _selectedGroupId = string.Empty;

    private DateTime _originalCreatedAt = DateTime.Now;

    /// <summary>편집 중인 프로젝트 Id (null이면 신규 추가)</summary>
    public string? EditingProjectId { get; private set; }

    /// <summary>수정 모드 여부</summary>
    public bool IsEditMode => EditingProjectId is not null;

    /// <summary>읽기 모드 여부</summary>
    [ObservableProperty]
    private bool _isViewMode;

    /// <summary>등록 날짜 표시 텍스트</summary>
    public string CreatedAtText { get; private set; } = string.Empty;

    /// <summary>중복 체크용 기존 프로젝트 이름 목록</summary>
    public IReadOnlyList<string> ExistingNames { get; private set; } = [];

    public ObservableCollection<ProjectGroup> Groups { get; } = [];

    /// <summary>AppSettings에서 등록된 외부 도구 목록</summary>
    public ObservableCollection<ExternalTool> DevTools { get; } = [];

    /// <summary>카테고리 선택 목록</summary>
    public string[] Categories { get; private set; } =
        [.. AppSettingsDialogViewModel.DefaultCategories];

    /// <summary>기술 스택 태그 선택 뱃지 목록</summary>
    public ObservableCollection<TagBadgeItem> AvailableTags { get; } = [];

    private IReadOnlyList<string> _customTags = [];

    /// <summary>AppSettings의 카테고리와 기본 카테고리를 병합하여 로드합니다.</summary>
    public void LoadCategories(IEnumerable<string> customCategories)
    {
        Categories = AppSettingsDialogViewModel.DefaultCategories
            .Concat(customCategories)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        OnPropertyChanged(nameof(Categories));

        // 선택된 카테고리가 없으면 첫 번째 항목을 기본값으로 선택
        if (string.IsNullOrEmpty(Category) && Categories.Length > 0)
            Category = Categories[0];
    }

    public void LoadCustomTags(IEnumerable<string> customTags)
    {
        _customTags = customTags.ToList();
        RefreshAvailableTags([]);
    }

    private void RefreshAvailableTags(IReadOnlyCollection<string> selectedTagNames)
    {
        var selectedSet = new HashSet<string>(selectedTagNames, StringComparer.OrdinalIgnoreCase);
        AvailableTags.Clear();

        var allTags = AppSettingsDialogViewModel.DefaultTechStackTags
            .Concat(_customTags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase);

        foreach (var tag in allTags)
            AvailableTags.Add(new TagBadgeItem(tag) { IsSelected = selectedSet.Contains(tag) });
    }

    partial void OnSelectedDevToolChanged(ExternalTool? value)
    {
        IsShellTool = value is not null && IsShellToolName(value.Name);
    }

    internal static bool IsShellToolName(string name)
        => name.Equals(PowerShellToolName, StringComparison.OrdinalIgnoreCase)
        || name.Equals(CmdToolName, StringComparison.OrdinalIgnoreCase);

    /// <summary>셸 도구 실행 폴더 선택 (WinUI 3 FolderPicker)</summary>
    [RelayCommand]
    private async Task BrowseShellWorkingDirectory()
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
            ShellWorkingDirectory = folder.Path;
    }

    /// <summary>폴더 찾아보기</summary>
    [RelayCommand]
    private async Task BrowseFolder()
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            Path = folder.Path;
            AutoFillNameIfEmpty();
        }
    }

    /// <summary>파일 찾아보기</summary>
    [RelayCommand]
    private async Task BrowseFile()
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            Path = file.Path;
            AutoFillNameIfEmpty();
        }
    }

    /// <summary>아이콘 이미지 파일 선택</summary>
    [RelayCommand]
    private async Task BrowseIcon()
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".ico");
        picker.FileTypeFilter.Add(".bmp");

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
            IconPath = file.Path;
    }

    [RelayCommand]
    private void ClearIcon() => IconPath = string.Empty;

    private void AutoFillNameIfEmpty()
    {
        if (string.IsNullOrWhiteSpace(Name))
            Name = System.IO.Path.GetFileNameWithoutExtension(Path.TrimEnd('\\', '/'));
    }

    /// <summary>입력 값으로 ProjectItem을 생성합니다.</summary>
    public ProjectItem ToProjectItem()
    {
        return new ProjectItem
        {
            Id = EditingProjectId ?? Guid.NewGuid().ToString(),
            Name = Name.Trim(),
            Description = Description.Trim(),
            IconPath = IconPath.Trim(),
            Path = IsShellTool ? string.Empty : Path.Trim(),
            DevToolName = SelectedDevTool?.Name ?? string.Empty,
            Options = IsShellTool ? string.Empty : Options.Trim(),
            Command = IsShellTool ? Command.Trim() : string.Empty,
            UseWorkingDirectory = IsShellTool && UseWorkingDirectory,
            ShellWorkingDirectory = IsShellTool ? ShellWorkingDirectory.Trim() : string.Empty,
            Tags = AvailableTags.Where(t => t.IsSelected).Select(t => t.Name)
                .Concat(Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.ToLowerInvariant()))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Category = Category,
            GroupId = SelectedGroupId,
            RunAsAdmin = RunAsAdmin,
            CreatedAt = _originalCreatedAt
        };
    }

    /// <summary>입력 유효성을 검증합니다. 유효하면 null, 오류 시 메시지를 반환합니다.</summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return LocalizationService.Get("ProjectNameInputRequired");

        var trimmedName = Name.Trim();

        if (ExistingNames.Any(n => n.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
            return string.Format(LocalizationService.Get("ProjectNameDuplicateFormat"), trimmedName);

        if (SelectedDevTool is null)
            return LocalizationService.Get("DevToolInputRequired");

        if (IsShellTool)
        {
            if (string.IsNullOrWhiteSpace(Command))
                return LocalizationService.Get("CommandInputRequired");

            if (UseWorkingDirectory && string.IsNullOrWhiteSpace(ShellWorkingDirectory))
                return LocalizationService.Get("ShellWorkingDirectoryRequired");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(Path) && string.IsNullOrWhiteSpace(Options))
                return LocalizationService.Get("ProjectPathOrOptionsRequired");
        }

        if (string.IsNullOrWhiteSpace(Category))
            return LocalizationService.Get("CategorySelectRequired");

        if (string.IsNullOrWhiteSpace(SelectedGroupId))
            return LocalizationService.Get("GroupSelectRequired");

        return null;
    }

    /// <summary>기존 프로젝트 데이터를 로드합니다 (수정 모드).</summary>
    public void LoadFrom(ProjectItem item, IEnumerable<ProjectGroup> groups, IEnumerable<ExternalTool> tools)
    {
        EditingProjectId = item.Id;
        _originalCreatedAt = item.CreatedAt;
        CreatedAtText = item.CreatedAt.ToString("yyyy-MM-dd HH:mm");
        Name = item.Name;
        Description = item.Description;
        IconPath = item.IconPath;
        Path = item.Path;
        Options = item.Options;
        Command = item.Command;
        UseWorkingDirectory = item.UseWorkingDirectory;
        ShellWorkingDirectory = item.ShellWorkingDirectory;
        RunAsAdmin = item.RunAsAdmin;

        var availableTagNames = new HashSet<string>(
            AppSettingsDialogViewModel.DefaultTechStackTags.Concat(_customTags),
            StringComparer.OrdinalIgnoreCase);
        var badgeTags = item.Tags.Where(t => availableTagNames.Contains(t)).ToList();
        var remainingTags = item.Tags.Where(t => !availableTagNames.Contains(t)).ToList();
        Tags = string.Join(", ", remainingTags);
        RefreshAvailableTags(badgeTags);
        Category = item.Category;
        SelectedGroupId = item.GroupId;
        LoadGroups(groups);
        LoadTools(tools);
        SelectedDevTool = DevTools.FirstOrDefault(t => t.Name == item.DevToolName);
    }

    public void LoadGroups(IEnumerable<ProjectGroup> groups)
    {
        Groups.Clear();
        foreach (var g in groups)
            Groups.Add(g);

        // 선택된 그룹이 없으면 첫 번째 항목을 기본값으로 선택
        if (string.IsNullOrEmpty(SelectedGroupId) && Groups.Count > 0)
            SelectedGroupId = Groups[0].Id;
    }

    public void SetExistingNames(IReadOnlyList<string> names)
    {
        ExistingNames = EditingProjectId is not null
            ? names.Where(n => !n.Equals(Name, StringComparison.OrdinalIgnoreCase)).ToList()
            : names;
    }

    public void LoadTools(IEnumerable<ExternalTool> tools)
    {
        DevTools.Clear();
        foreach (var t in tools)
            DevTools.Add(t);
    }
}

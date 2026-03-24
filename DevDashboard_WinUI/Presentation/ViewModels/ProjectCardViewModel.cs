using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.Models;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>개별 프로젝트 카드 뷰모델</summary>
public partial class ProjectCardViewModel : ObservableObject
{
    private readonly ProjectItem _item;
    private readonly IReadOnlyList<ExternalTool> _tools;
    private readonly IProjectRepository _repository;
    private readonly AppSettings _settings;

    // 동일 아이콘 경로의 중복 로드 방지 — Refresh 시 ClearIconCache()로 정리
    private static readonly ConcurrentDictionary<string, Lazy<Task<BitmapImage?>>> _iconLoadTasks
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>아이콘 캐시 최대 크기 — 초과 시 자동 정리</summary>
    private const int MaxIconCacheSize = 500;

    // git 프로세스 동시 생성 비용이 크므로 최대 8개로 제한
    private static readonly SemaphoreSlim _gitProbeSemaphore = new(8, 8);
    // 파일 I/O는 git보다 빠르므로 최대 16개 허용
    private static readonly SemaphoreSlim _validationSemaphore = new(16, 16);

    /// <summary>아이콘 캐시를 비웁니다. Refresh/HardRefresh 시 호출하여 메모리 누적을 방지합니다.</summary>
    internal static void ClearIconCache() => _iconLoadTasks.Clear();

    // Todos/Histories/Tests 지연 로딩 플래그 — 다이얼로그 최초 열기 시 DB에서 로드
    private bool _todosLoaded;
    private bool _historiesLoaded;
    private bool _testsLoaded;

    /// <summary>설정된 개발 도구가 유효한지 여부</summary>
    [ObservableProperty]
    public partial bool IsDevToolValid { get; set; } = true;

    /// <summary>프로젝트 경로가 유효한지 여부</summary>
    [ObservableProperty]
    public partial bool IsProjectPathValid { get; set; } = true;

    /// <summary>커맨드 스크립트 슬롯 수</summary>
    public const int CommandSlotCount = 4;

    /// <summary>태그 마키 애니메이션 활성화 여부 — AppSettings에서 전달</summary>
    [ObservableProperty]
    public partial bool EnableTagAnimation { get; set; }

    [ObservableProperty]
    public partial bool IsPinned { get; set; }

    [ObservableProperty]
    public partial BitmapImage? IconSource { get; set; }

    // --- Git 상태 ---

    /// <summary>git repo 여부 — false이면 Git 버튼 숨김</summary>
    [ObservableProperty]
    public partial bool IsGitRepo { get; set; }

    /// <summary>현재 브랜치명</summary>
    [ObservableProperty]
    public partial string GitBranch { get; set; } = string.Empty;

    /// <summary>날짜별로 그룹화된 커밋 목록</summary>
    [ObservableProperty]
    public partial IReadOnlyList<GitCommitGroup> GitCommitGroups { get; set; } = [];

    // --- 커맨드 스크립트 슬롯별 설정 상태 ---

    [ObservableProperty]
    public partial bool IsCmd0Configured { get; set; }

    [ObservableProperty]
    public partial bool IsCmd1Configured { get; set; }

    [ObservableProperty]
    public partial bool IsCmd2Configured { get; set; }

    [ObservableProperty]
    public partial bool IsCmd3Configured { get; set; }

    [ObservableProperty]
    public partial string Cmd0Tooltip { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Cmd1Tooltip { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Cmd2Tooltip { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Cmd3Tooltip { get; set; } = string.Empty;

    // --- 커맨드 스크립트 슬롯별 아이콘 글리프 (Segoe MDL2 Assets 유니코드) ---

    [ObservableProperty]
    public partial string Cmd0Icon { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Cmd1Icon { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Cmd2Icon { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Cmd3Icon { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasCmd0Icon { get; set; }

    [ObservableProperty]
    public partial bool HasCmd1Icon { get; set; }

    [ObservableProperty]
    public partial bool HasCmd2Icon { get; set; }

    [ObservableProperty]
    public partial bool HasCmd3Icon { get; set; }

    [ObservableProperty]
    public partial bool IsCmd0RunAsAdmin { get; set; }

    [ObservableProperty]
    public partial bool IsCmd1RunAsAdmin { get; set; }

    [ObservableProperty]
    public partial bool IsCmd2RunAsAdmin { get; set; }

    [ObservableProperty]
    public partial bool IsCmd3RunAsAdmin { get; set; }

    // --- 슬롯 표시 여부 ---

    public bool IsCmd1SlotVisible => IsCmd0Configured || IsCmd1Configured;
    public bool IsCmd2SlotVisible => IsCmd1Configured || IsCmd2Configured;
    public bool IsCmd3SlotVisible => IsCmd2Configured || IsCmd3Configured;

    partial void OnIsCmd0ConfiguredChanged(bool value) => OnPropertyChanged(nameof(IsCmd1SlotVisible));
    partial void OnIsCmd1ConfiguredChanged(bool value)
    {
        OnPropertyChanged(nameof(IsCmd1SlotVisible));
        OnPropertyChanged(nameof(IsCmd2SlotVisible));
    }
    partial void OnIsCmd2ConfiguredChanged(bool value)
    {
        OnPropertyChanged(nameof(IsCmd2SlotVisible));
        OnPropertyChanged(nameof(IsCmd3SlotVisible));
    }
    partial void OnIsCmd3ConfiguredChanged(bool value) => OnPropertyChanged(nameof(IsCmd3SlotVisible));

    public string Id => _item.Id;
    public string Name => _item.Name;
    public string Description => _item.Description;
    public string IconPath => _item.IconPath;
    public string Path => _item.Path;
    public string DevToolName => _item.DevToolName;
    public string Options => _item.Options;
    public string Command => _item.Command;
    public IReadOnlyList<string> Tags => _item.Tags;
    public string GitStatus => _item.GitStatus;
    public string GroupId => _item.GroupId;
    public string Category => _item.Category;
    public DateTime CreatedAt => _item.CreatedAt;
    public bool RunAsAdmin => _item.RunAsAdmin;

    /// <summary>완료되지 않은 활성 To-Do 항목이 있는지 여부</summary>
    public bool HasInProgressTodo => _item.HasActiveTodo;

    /// <summary>완료되지 않은 활성 테스트 항목이 있는지 여부</summary>
    public bool HasInProgressTest => _item.HasActiveTest;

    // --- View에서 처리할 다이얼로그 요청 이벤트 ---

    /// <summary>삭제 요청 이벤트</summary>
    public event EventHandler? DeleteRequested;

    /// <summary>편집 요청 이벤트</summary>
    public event EventHandler? EditRequested;

    /// <summary>핀 상태 변경 이벤트</summary>
    public event EventHandler? PinToggled;

    /// <summary>커맨드 스크립트 변경 이벤트</summary>
    public event EventHandler? CommandScriptChanged;

    /// <summary>To-Do 변경 이벤트</summary>
    public event EventHandler? TodoChanged;

    /// <summary>작업 기록 변경 이벤트</summary>
    public event EventHandler? HistoryChanged;

    /// <summary>Git 상태 다이얼로그 표시 요청 이벤트</summary>
    public event EventHandler? ShowGitStatusRequested;

    /// <summary>To-Do 다이얼로그 표시 요청 이벤트</summary>
    public event EventHandler? OpenTodoRequested;

    /// <summary>작업 기록 다이얼로그 표시 요청 이벤트</summary>
    public event EventHandler? OpenHistoryRequested;

    /// <summary>테스트 목록 다이얼로그 표시 요청 이벤트</summary>
    public event EventHandler? OpenTestListRequested;

    /// <summary>테스트 목록 변경 이벤트</summary>
    public event EventHandler? TestChanged;

    /// <summary>커맨드 슬롯 설정 요청 이벤트 (슬롯 인덱스)</summary>
    public event EventHandler<int>? ConfigureCommandSlotRequested;

    /// <summary>커맨드 아이콘 변경 요청 이벤트 (슬롯 인덱스)</summary>
    public event EventHandler<int>? ChangeCommandIconRequested;

    public ProjectCardViewModel(ProjectItem item, IReadOnlyList<ExternalTool> tools,
        IProjectRepository repository, AppSettings settings)
    {
        _item = item;
        _tools = tools;
        _repository = repository;
        _settings = settings;
        IsPinned = item.IsPinned;
        EnableTagAnimation = settings.EnableTagAnimation;
        RefreshCommandSlotStates();
    }

    /// <summary>아이콘 비동기 로드를 시작합니다.</summary>
    public void StartIconLoad() => _ = SafeFireAndForget(LoadIconAsync());

    /// <summary>Git 상태 비동기 로드를 시작합니다.</summary>
    public void StartGitStatusLoad() => _ = SafeFireAndForget(ProbeGitRepositoryAsync());

    /// <summary>파일 시스템 유효성 검사를 비동기로 시작합니다.</summary>
    public void StartValidation() => _ = SafeFireAndForget(ValidatePathsAsync());

    /// <summary>fire-and-forget 비동기 작업의 예외를 안전하게 처리합니다.</summary>
    private static async Task SafeFireAndForget(Task task)
    {
        try { await task; }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ProjectCard] 백그라운드 작업 오류: {ex.Message}"); }
    }

    private async Task ProbeGitRepositoryAsync()
    {
        var workingDir = ResolveWorkingDirectory(Path);
        if (workingDir is null) { IsGitRepo = false; return; }

        await _gitProbeSemaphore.WaitAsync();
        try
        {
            var (isRepo, _) = await GitHelper.IsRepositoryAsync(workingDir);
            IsGitRepo = isRepo;
        }
        finally
        {
            _gitProbeSemaphore.Release();
        }
    }

    /// <summary>Git 상태를 비동기로 로드합니다. View에서 호출합니다.</summary>
    public async Task<string?> LoadGitStatusAsync(CancellationToken ct = default)
    {
        var workingDir = ResolveWorkingDirectory(Path);
        if (workingDir is null) return LocalizationService.Get("GitPathNotResolved");

        var (repositoryRoot, rootError) = await GitHelper.GetRepositoryRootAsync(workingDir, ct);
        if (!string.IsNullOrWhiteSpace(rootError)) return rootError;
        var gitWorkingDir = string.IsNullOrWhiteSpace(repositoryRoot) ? workingDir : repositoryRoot;

        var (branch, branchError) = await GitHelper.GetBranchAsync(gitWorkingDir, ct);
        var (commits, commitsError) = await GitHelper.GetCommitsAsync(gitWorkingDir, ct);
        if (!string.IsNullOrWhiteSpace(commitsError)) return commitsError;
        if (!string.IsNullOrWhiteSpace(branchError)) return branchError;

        GitBranch = string.IsNullOrWhiteSpace(branch) ? "(detached HEAD)" : branch;
        GitCommitGroups = GroupCommitsByDate(commits);
        return null;
    }

    private static IReadOnlyList<GitCommitGroup> GroupCommitsByDate(IReadOnlyList<GitCommit> commits)
    {
        return [.. commits
            .GroupBy(c => c.Date.LocalDateTime.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new GitCommitGroup(g.Key.ToString("yyyy-MM-dd"), g))];
    }

    private static string? ResolveWorkingDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (Directory.Exists(path)) return path;
        var dir = System.IO.Path.GetDirectoryName(path);
        return Directory.Exists(dir) ? dir : null;
    }

    /// <summary>Todos가 아직 로드되지 않았으면 DB에서 로드합니다.</summary>
    public void EnsureTodosLoaded()
    {
        if (_todosLoaded) return;
        _item.Todos = _repository.GetTodos(_item.Id);
        _todosLoaded = true;
    }

    /// <summary>Histories가 아직 로드되지 않았으면 DB에서 로드합니다.</summary>
    public void EnsureHistoriesLoaded()
    {
        if (_historiesLoaded) return;
        _item.Histories = _repository.GetHistories(_item.Id);
        _historiesLoaded = true;
    }

    /// <summary>TestCategories가 아직 로드되지 않았으면 DB에서 로드합니다.</summary>
    public void EnsureTestsLoaded()
    {
        if (_testsLoaded) return;
        _item.TestCategories = _repository.GetTestCategories(_item.Id);
        _testsLoaded = true;
    }

    private async Task LoadIconAsync()
    {
        if (string.IsNullOrWhiteSpace(IconPath))
            return;

        // 캐시 크기 초과 시 전체 정리하여 메모리 누적 방지
        if (_iconLoadTasks.Count > MaxIconCacheSize)
            _iconLoadTasks.Clear();

        var lazy = _iconLoadTasks.GetOrAdd(IconPath,
            path => new Lazy<Task<BitmapImage?>>(() => LoadBitmapAsync(path)));
        var result = await lazy.Value;
        IconSource = result;
    }

    private static async Task<BitmapImage?> LoadBitmapAsync(string path)
    {
        // 파일 존재 여부를 백그라운드에서 확인
        var exists = await Task.Run(() => File.Exists(path));
        if (!exists) return null;

        try
        {
            // BitmapImage는 UI 스레드에서 생성 (await 후 SynchronizationContext 복귀)
            return new BitmapImage(new Uri(path));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>파일 시스템 유효성 검사를 수행합니다.</summary>
    public void ValidatePathsSync()
    {
        IsDevToolValid = CheckDevToolValid();
        IsProjectPathValid = CheckProjectPathValid();
        OpenFolderCommand.NotifyCanExecuteChanged();
    }

    private async Task ValidatePathsAsync()
    {
        await _validationSemaphore.WaitAsync();
        try
        {
            var devToolValid = await Task.Run(CheckDevToolValid);
            var projectPathValid = await Task.Run(CheckProjectPathValid);
            IsDevToolValid = devToolValid;
            IsProjectPathValid = projectPathValid;
            OpenFolderCommand.NotifyCanExecuteChanged();
        }
        finally
        {
            _validationSemaphore.Release();
        }
    }

    /// <summary>GroupId를 변경합니다.</summary>
    public void UpdateGroupId(string newGroupId) => _item.GroupId = newGroupId;

    /// <summary>현재 상태를 모델에 반영하고 반환합니다.</summary>
    public ProjectItem ToModel()
    {
        _item.IsPinned = IsPinned;
        return _item;
    }

    [RelayCommand]
    private void TogglePin()
    {
        IsPinned = !IsPinned;
        _item.IsPinned = IsPinned;
        PinToggled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>설정된 개발 도구로 프로젝트를 엽니다.</summary>
    [RelayCommand]
    private void OpenInDevTool() => LaunchDevTool(RunAsAdmin);

    /// <summary>관리자 권한으로 개발 도구를 실행합니다.</summary>
    [RelayCommand]
    private void OpenInDevToolAsAdmin() => LaunchDevTool(runAsAdmin: true);

    private void LaunchDevTool(bool runAsAdmin)
    {
        if (string.IsNullOrWhiteSpace(DevToolName)) return;

        if (DevToolName.Equals(ProjectSettingsDialogViewModel.PowerShellToolName, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(Command)) return;

            if (_item.UseWorkingDirectory)
            {
                if (!Directory.Exists(_item.ShellWorkingDirectory))
                {
                    ShowWorkingDirectoryNotFound(_item.ShellWorkingDirectory);
                    return;
                }
                TryStartProcess("powershell.exe",
                    $"-NoExit -Command \"Set-Location '{_item.ShellWorkingDirectory}'; {Command}\"", runAsAdmin);
            }
            else
            {
                TryStartProcess("powershell.exe", $"-NoExit -Command \"{Command}\"", runAsAdmin);
            }
            return;
        }

        if (DevToolName.Equals(ProjectSettingsDialogViewModel.CmdToolName, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(Command)) return;

            if (_item.UseWorkingDirectory)
            {
                if (!Directory.Exists(_item.ShellWorkingDirectory))
                {
                    ShowWorkingDirectoryNotFound(_item.ShellWorkingDirectory);
                    return;
                }
                TryStartProcess("cmd.exe",
                    $"/k cd /d \"{_item.ShellWorkingDirectory}\" && {Command}", runAsAdmin);
            }
            else
            {
                TryStartProcess("cmd.exe", $"/k {Command}", runAsAdmin);
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(Path)) return;

        var tool = _tools.FirstOrDefault(t => t.Name == DevToolName);
        if (tool is null || string.IsNullOrWhiteSpace(tool.ExecutablePath)) return;

        var arguments = string.IsNullOrWhiteSpace(Options)
            ? $"\"{Path}\""
            : $"{Options} \"{Path}\"";

        TryStartProcess(tool.ExecutablePath, arguments, runAsAdmin);
    }

    [RelayCommand]
    private void OpenTerminal()
    {
        var folderPath = ResolveTerminalFolder();
        TryStartProcess("cmd.exe", folderPath is not null ? $"/k cd /d \"{folderPath}\"" : string.Empty);
    }

    [RelayCommand]
    private void OpenTerminalWithPowerShell()
    {
        var folderPath = ResolveTerminalFolder();
        TryStartProcess("powershell.exe", folderPath is not null ? $"-NoExit -Command \"Set-Location '{folderPath}'\"" : string.Empty);
    }

    private string? ResolveTerminalFolder()
    {
        if (string.IsNullOrWhiteSpace(Path)) return null;
        if (Directory.Exists(Path)) return Path;
        var dir = System.IO.Path.GetDirectoryName(Path);
        return !string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir) ? dir : null;
    }

    [RelayCommand(CanExecute = nameof(CanOpenFolder))]
    private void OpenFolder()
    {
        var folderPath = Directory.Exists(Path) ? Path : System.IO.Path.GetDirectoryName(Path);
        TryStartProcess("explorer.exe", folderPath!);
    }

    private bool CanOpenFolder()
    {
        if (string.IsNullOrWhiteSpace(Path)) return false;
        if (Directory.Exists(Path)) return true;
        var dir = System.IO.Path.GetDirectoryName(Path);
        return !string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir);
    }

    [RelayCommand]
    private void Edit() => EditRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void Delete() => DeleteRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ShowGitStatus() => ShowGitStatusRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenTodo() => OpenTodoRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenHistory() => OpenHistoryRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenTestList() => OpenTestListRequested?.Invoke(this, EventArgs.Empty);

    // --- To-Do / History 다이얼로그 결과 처리 (View에서 호출) ---

    /// <summary>To-Do 다이얼로그 뷰모델을 생성합니다.</summary>
    public TodoDialogViewModel CreateTodoDialogViewModel()
    {
        EnsureTodosLoaded();
        return new TodoDialogViewModel(_item);
    }

    /// <summary>To-Do 다이얼로그가 닫힌 후 결과를 반영합니다.</summary>
    public void OnTodoDialogClosed(TodoDialogViewModel dialogVm, IList<HistoryEntry>? newHistories = null)
    {
        dialogVm.SaveToModel();
        TodoChanged?.Invoke(this, EventArgs.Empty);

        if (newHistories is { Count: > 0 })
        {
            EnsureHistoriesLoaded();
            _item.Histories ??= [];
            _item.Histories.AddRange(newHistories);
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        _item.HasActiveTodo = _item.Todos?.Any(t => !t.IsCompleted) == true;
        OnPropertyChanged(nameof(HasInProgressTodo));
    }

    /// <summary>작업 기록 다이얼로그 뷰모델을 생성합니다.</summary>
    public HistoryDialogViewModel CreateHistoryDialogViewModel()
    {
        EnsureHistoriesLoaded();
        return new HistoryDialogViewModel(_item);
    }

    /// <summary>작업 기록 다이얼로그가 닫힌 후 결과를 반영합니다.</summary>
    public void OnHistoryDialogClosed(HistoryDialogViewModel dialogVm)
    {
        dialogVm.SaveToModel();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>테스트 목록 다이얼로그 뷰모델을 생성합니다.</summary>
    public TestListDialogViewModel CreateTestListDialogViewModel()
    {
        EnsureTestsLoaded();
        return new TestListDialogViewModel(_item);
    }

    /// <summary>테스트 목록 다이얼로그가 닫힌 후 결과를 반영합니다.</summary>
    public void OnTestListDialogClosed(TestListDialogViewModel dialogVm)
    {
        dialogVm.SaveToModel();
        TestChanged?.Invoke(this, EventArgs.Empty);

        _item.HasActiveTest = _item.TestCategories?.Any(c => c.Items.Any(t => !t.IsCompleted)) == true;
        OnPropertyChanged(nameof(HasInProgressTest));
    }

    // --- 커맨드 스크립트 슬롯 ---

    /// <summary>커맨드 버튼 클릭 — 설정 없으면 설정 다이얼로그, 있으면 실행</summary>
    [RelayCommand]
    private void ExecuteCommandSlot(string indexStr)
    {
        if (!TryParseSlotIndex(indexStr, out var index)) return;

        var script = GetCommandScript(index);
        if (script is null)
        {
            ConfigureCommandSlotRequested?.Invoke(this, index);
            return;
        }

        LaunchCommandScript(script);
    }

    /// <summary>커맨드 슬롯 설정 다이얼로그 표시 요청</summary>
    [RelayCommand]
    private void ConfigureCommandSlot(string indexStr)
    {
        if (!TryParseSlotIndex(indexStr, out var index)) return;
        ConfigureCommandSlotRequested?.Invoke(this, index);
    }

    /// <summary>커맨드 슬롯 삭제 — 뒤의 슬롯을 앞으로 당겨 빈 틈 없이 정렬</summary>
    [RelayCommand]
    private void ClearCommandSlot(string indexStr)
    {
        if (!TryParseSlotIndex(indexStr, out var index)) return;
        if (index >= _item.CommandScripts.Count) return;

        _item.CommandScripts.RemoveAt(index);
        _item.CommandScripts.Add(null);

        RefreshCommandSlotStates();
        CommandScriptChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>커맨드 슬롯 아이콘 변경 요청</summary>
    [RelayCommand]
    private void ChangeCommandIcon(string indexStr)
    {
        if (!TryParseSlotIndex(indexStr, out var index)) return;
        if (GetCommandScript(index) is null) return;
        ChangeCommandIconRequested?.Invoke(this, index);
    }

    /// <summary>커맨드 슬롯 인덱스 문자열을 파싱합니다. 유효 범위(0~CommandSlotCount-1) 벗어나면 false.</summary>
    private static bool TryParseSlotIndex(string indexStr, out int index)
        => int.TryParse(indexStr, out index) && index >= 0 && index < CommandSlotCount;

    /// <summary>슬롯 인덱스의 CommandScript를 반환합니다. (View에서 다이얼로그 전달용)</summary>
    public CommandScript? GetCommandScriptForDialog(int index) => GetCommandScript(index);

    /// <summary>View에서 다이얼로그 결과를 받아 커맨드 스크립트를 설정합니다.</summary>
    public void ApplyCommandScriptResult(int index, CommandScript result)
    {
        SetCommandScript(index, result);
        RefreshCommandSlotStates();
        CommandScriptChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>View에서 다이얼로그 결과를 받아 커맨드 아이콘 글리프를 설정합니다.</summary>
    public void ApplyCommandIconResult(int index, string glyph)
    {
        var script = GetCommandScript(index);
        if (script is null) return;
        script.IconSymbol = glyph;
        RefreshCommandSlotStates();
        CommandScriptChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void LaunchCommandScript(CommandScript script)
    {
        if (script.UseWorkingDirectory && !Directory.Exists(script.WorkingDirectory))
        {
            _ = DialogService.ShowErrorAsync(
                string.Format(LocalizationService.Get("WorkingDirectoryNotFoundFormat"),
                    Environment.NewLine, script.WorkingDirectory),
                LocalizationService.Get("WorkingDirectoryError"));
            return;
        }

        string fileName;
        string arguments;

        if (script.ShellType == ShellType.PowerShell)
        {
            fileName = "powershell.exe";
            arguments = script.UseWorkingDirectory
                ? $"-NoExit -Command \"Set-Location '{script.WorkingDirectory}'; {script.Script}\""
                : $"-NoExit -Command \"{script.Script}\"";
        }
        else
        {
            fileName = "cmd.exe";
            arguments = script.UseWorkingDirectory
                ? $"/k cd /d \"{script.WorkingDirectory}\" && {script.Script}"
                : $"/k {script.Script}";
        }

        TryStartProcess(fileName, arguments, script.RunAsAdmin);
    }

    private CommandScript? GetCommandScript(int index)
    {
        if (index < 0 || index >= _item.CommandScripts.Count) return null;
        return _item.CommandScripts[index];
    }

    private void SetCommandScript(int index, CommandScript? script)
    {
        while (_item.CommandScripts.Count <= index)
            _item.CommandScripts.Add(null);

        _item.CommandScripts[index] = script;
    }

    /// <summary>각 슬롯의 설정 상태, 툴팁, 아이콘을 갱신합니다.</summary>
    private void RefreshCommandSlotStates()
    {
        for (int i = 0; i < CommandSlotCount; i++)
        {
            var script = GetCommandScript(i);
            var configured = script is not null;
            var tooltip = configured
                ? script!.Description
                : LocalizationService.Get("CmdSlotEmptyTooltip");
            ParseSlotIcon(script, out var glyph, out var hasIcon);

            switch (i)
            {
                case 0:
                    IsCmd0Configured = configured; Cmd0Tooltip = tooltip;
                    Cmd0Icon = glyph; HasCmd0Icon = hasIcon;
                    IsCmd0RunAsAdmin = script?.RunAsAdmin ?? false;
                    break;
                case 1:
                    IsCmd1Configured = configured; Cmd1Tooltip = tooltip;
                    Cmd1Icon = glyph; HasCmd1Icon = hasIcon;
                    IsCmd1RunAsAdmin = script?.RunAsAdmin ?? false;
                    break;
                case 2:
                    IsCmd2Configured = configured; Cmd2Tooltip = tooltip;
                    Cmd2Icon = glyph; HasCmd2Icon = hasIcon;
                    IsCmd2RunAsAdmin = script?.RunAsAdmin ?? false;
                    break;
                case 3:
                    IsCmd3Configured = configured; Cmd3Tooltip = tooltip;
                    Cmd3Icon = glyph; HasCmd3Icon = hasIcon;
                    IsCmd3RunAsAdmin = script?.RunAsAdmin ?? false;
                    break;
            }
        }
    }

    /// <summary>커맨드 스크립트에서 아이콘 글리프를 파싱합니다.</summary>
    private static void ParseSlotIcon(CommandScript? script, out string glyph, out bool hasIcon)
    {
        if (script is not null && !string.IsNullOrWhiteSpace(script.IconSymbol))
        {
            glyph = script.IconSymbol;
            hasIcon = true;
        }
        else
        {
            glyph = string.Empty;
            hasIcon = false;
        }
    }

    private static void TryStartProcess(string fileName, string arguments, bool runAsAdmin = false)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true,
                Verb = runAsAdmin ? "runas" : string.Empty
            })?.Dispose();
        }
        catch (OperationCanceledException)
        {
            // 관리자 권한 UAC 취소 — 정상 동작으로 무시
        }
        catch (Exception ex)
        {
            _ = DialogService.ShowErrorAsync(
                string.Format(LocalizationService.Get("LaunchFailedFormat"), Environment.NewLine, ex.Message),
                LocalizationService.Get("LaunchError"));
        }
    }

    private static void ShowWorkingDirectoryNotFound(string path)
    {
        _ = DialogService.ShowErrorAsync(
            string.Format(LocalizationService.Get("WorkingDirectoryNotFoundFormat"), Environment.NewLine, path),
            LocalizationService.Get("WorkingDirectoryError"));
    }

    private bool CheckDevToolValid()
    {
        if (string.IsNullOrWhiteSpace(DevToolName))
            return false;

        if (ProjectSettingsDialogViewModel.IsShellToolName(DevToolName))
            return true;

        var tool = _tools.FirstOrDefault(t => t.Name == DevToolName);
        if (tool is null || string.IsNullOrWhiteSpace(tool.ExecutablePath))
            return false;

        return File.Exists(tool.ExecutablePath);
    }

    private bool CheckProjectPathValid()
    {
        if (string.IsNullOrWhiteSpace(DevToolName))
            return true;

        if (ProjectSettingsDialogViewModel.IsShellToolName(DevToolName))
            return true;

        if (string.IsNullOrWhiteSpace(Path))
            return false;

        return File.Exists(Path) || Directory.Exists(Path);
    }
}

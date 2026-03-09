using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevDashboard.Infrastructure.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>커맨드 스크립트 설정 다이얼로그 뷰모델</summary>
public partial class CommandScriptDialogViewModel : ObservableObject
{
    /// <summary>셸 타입 선택 목록</summary>
    public ShellType[] ShellTypes { get; } = [ShellType.Cmd, ShellType.PowerShell];

    /// <summary>스크립트 설명</summary>
    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    /// <summary>선택된 셸 타입</summary>
    [ObservableProperty]
    public partial ShellType SelectedShellType { get; set; } = ShellType.Cmd;

    /// <summary>관리자 권한으로 실행 여부</summary>
    [ObservableProperty]
    public partial bool RunAsAdmin { get; set; }

    /// <summary>실행할 스크립트 내용</summary>
    [ObservableProperty]
    public partial string Script { get; set; } = string.Empty;

    /// <summary>실행 폴더 사용 여부</summary>
    [ObservableProperty]
    public partial bool UseWorkingDirectory { get; set; }

    /// <summary>실행 폴더 경로</summary>
    [ObservableProperty]
    public partial string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>기존 아이콘 글리프 (편집 시 보존용)</summary>
    private string _iconSymbol = string.Empty;

    /// <summary>기존 설정을 로드합니다.</summary>
    public void LoadFrom(CommandScript source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Description = source.Description;
        SelectedShellType = source.ShellType;
        RunAsAdmin = source.RunAsAdmin;
        Script = source.Script;
        UseWorkingDirectory = source.UseWorkingDirectory;
        WorkingDirectory = source.WorkingDirectory;
        _iconSymbol = source.IconSymbol;
    }

    /// <summary>입력값을 CommandScript로 변환합니다.</summary>
    public CommandScript ToCommandScript()
    {
        return new CommandScript
        {
            Description = Description.Trim(),
            ShellType = SelectedShellType,
            RunAsAdmin = RunAsAdmin,
            Script = Script,
            UseWorkingDirectory = UseWorkingDirectory,
            WorkingDirectory = WorkingDirectory.Trim(),
            IconSymbol = _iconSymbol
        };
    }

    /// <summary>입력 유효성을 검증합니다. 유효하면 null, 오류 시 메시지를 반환합니다.</summary>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Description))
            return LocalizationService.Get("ScriptDescriptionRequired");

        if (string.IsNullOrWhiteSpace(Script))
            return LocalizationService.Get("ScriptRequired");

        if (UseWorkingDirectory && string.IsNullOrWhiteSpace(WorkingDirectory))
            return LocalizationService.Get("WorkingDirectoryRequired");

        return null;
    }

    /// <summary>실행 폴더 선택 다이얼로그 (WinUI 3 FolderPicker)</summary>
    [RelayCommand]
    private async Task BrowseWorkingDirectory()
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
            WorkingDirectory = folder.Path;
    }
}

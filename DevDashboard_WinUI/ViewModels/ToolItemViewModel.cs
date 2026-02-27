using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevDashboard.Models;
using DevDashboard.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace DevDashboard.ViewModels;

/// <summary>도구 목록 아코디언 항목 뷰모델</summary>
public partial class ToolItemViewModel : ObservableObject
{
    private readonly Action<ToolItemViewModel> _onRequestExpand;
    private readonly Action<ToolItemViewModel> _onRequestDelete;
    private readonly Func<ToolItemViewModel, string, bool> _onCheckDuplicateName;

    /// <summary>신규 추가 항목 여부 (저장 버튼 클릭 전까지 true)</summary>
    public bool IsNew { get; private set; }

    /// <summary>도구 표시 이름</summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>실행 파일 경로</summary>
    [ObservableProperty]
    private string _executablePath = string.Empty;

    /// <summary>아코디언 확장 여부</summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>편집 모드 여부</summary>
    [ObservableProperty]
    private bool _isEditing;

    /// <summary>편집 중인 도구 이름</summary>
    [ObservableProperty]
    private string _editName = string.Empty;

    /// <summary>편집 중인 실행 파일 경로</summary>
    [ObservableProperty]
    private string _editExecutablePath = string.Empty;

    public ToolItemViewModel(
        Action<ToolItemViewModel> onRequestExpand,
        Action<ToolItemViewModel> onRequestDelete,
        Func<ToolItemViewModel, string, bool> onCheckDuplicateName)
    {
        ArgumentNullException.ThrowIfNull(onRequestExpand);
        ArgumentNullException.ThrowIfNull(onRequestDelete);
        ArgumentNullException.ThrowIfNull(onCheckDuplicateName);

        _onRequestExpand = onRequestExpand;
        _onRequestDelete = onRequestDelete;
        _onCheckDuplicateName = onCheckDuplicateName;
    }

    /// <summary>헤더 클릭 시 토글 확장/축소</summary>
    [RelayCommand]
    private void ToggleExpand()
    {
        if (IsExpanded)
            IsExpanded = false;
        else
            _onRequestExpand(this);
    }

    /// <summary>편집 모드 진입</summary>
    [RelayCommand]
    private void StartEdit()
    {
        EditName = Name;
        EditExecutablePath = ExecutablePath;
        IsEditing = true;
    }

    /// <summary>편집 저장 시 표시할 오류 메시지 (인라인 표시)</summary>
    [ObservableProperty]
    private string _editErrorText = string.Empty;

    /// <summary>편집 저장</summary>
    [RelayCommand]
    private void SaveEdit()
    {
        EditErrorText = string.Empty;

        if (string.IsNullOrWhiteSpace(EditName) || string.IsNullOrWhiteSpace(EditExecutablePath))
        {
            EditErrorText = LocalizationService.Get("ToolInputRequired");
            return;
        }

        var trimmedName = EditName.Trim();

        if (_onCheckDuplicateName(this, trimmedName))
        {
            EditErrorText = LocalizationService.Get("NameAlreadyRegistered");
            return;
        }

        Name = trimmedName;
        ExecutablePath = EditExecutablePath;
        IsEditing = false;
        IsNew = false;
    }

    /// <summary>편집 취소</summary>
    [RelayCommand]
    private void CancelEdit()
    {
        if (IsNew)
        {
            _onRequestDelete(this);
        }
        else
        {
            EditName = Name;
            EditExecutablePath = ExecutablePath;
            IsEditing = false;
        }
    }

    /// <summary>신규 추가 항목으로 표시</summary>
    public void MarkAsNew() => IsNew = true;

    /// <summary>삭제 요청</summary>
    [RelayCommand]
    private void Delete()
    {
        _onRequestDelete(this);
    }

    /// <summary>실행 파일 선택 다이얼로그 (WinUI 3 FileOpenPicker)</summary>
    [RelayCommand]
    private async Task BrowseExecutable()
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".exe");
        picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;

        // WinUI 3에서는 hwnd 초기화 필요
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            EditExecutablePath = file.Path;
        }
    }

    /// <summary>ExternalTool 모델로부터 값 로드</summary>
    public void LoadFrom(ExternalTool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        Name = tool.Name;
        ExecutablePath = tool.ExecutablePath;
    }

    /// <summary>ExternalTool 모델로 값 내보내기</summary>
    public ExternalTool ToModel() => new()
    {
        Name = Name,
        ExecutablePath = ExecutablePath
    };
}

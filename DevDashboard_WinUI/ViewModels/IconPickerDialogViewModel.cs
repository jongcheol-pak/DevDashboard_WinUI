using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DevDashboard.ViewModels;

/// <summary>아이콘 항목 — 이름(검색용)과 글리프(Segoe MDL2 Assets 유니코드 문자)</summary>
public sealed record IconItem(string Name, string Glyph);

/// <summary>아이콘 선택 다이얼로그 뷰모델 — Segoe MDL2 Assets 아이콘 목록 표시</summary>
public partial class IconPickerDialogViewModel : ObservableObject
{
    private static readonly IReadOnlyList<IconItem> _allIcons = BuildIconList();
    private CancellationTokenSource? _searchCts;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<IconItem> _filteredIcons = [];

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private int _totalIconCount;

    [ObservableProperty]
    private int _visibleIconCount;

    /// <summary>선택된 아이콘 글리프 문자 (null이면 선택 취소/기본값)</summary>
    public string? SelectedGlyph { get; private set; }

    /// <summary>다이얼로그 결과 확정 여부</summary>
    public bool IsConfirmed { get; private set; }

    /// <summary>기본값 버튼 클릭 여부</summary>
    public bool IsDefault { get; private set; }

    /// <summary>다이얼로그 닫기 요청 이벤트</summary>
    public event EventHandler? CloseRequested;

    /// <summary>다이얼로그 표시 후 호출하여 아이콘을 비동기로 로드합니다.</summary>
    public async Task LoadIconsAsync()
    {
        IsLoading = true;
        TotalIconCount = _allIcons.Count;
        await ApplyFilterAsync(CancellationToken.None);
        IsLoading = false;
    }

    /// <summary>검색어 변경 시 필터 적용</summary>
    public async Task UpdateSearchAsync(string text)
    {
        SearchText = text;

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        IsLoading = true;
        try
        {
            await ApplyFilterAsync(token);
        }
        catch (OperationCanceledException)
        {
            // 최신 검색 요청만 반영하기 위해 취소는 정상 흐름으로 처리
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsLoading = false;
        }
    }

    /// <summary>검색어 기반 필터링</summary>
    private async Task ApplyFilterAsync(CancellationToken cancellationToken)
    {
        var query = SearchText?.Trim() ?? string.Empty;

        var filtered = await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<IconItem> result = _allIcons;
            if (!string.IsNullOrWhiteSpace(query))
                result = result.Where(i => i.Name.Contains(query, StringComparison.OrdinalIgnoreCase));

            return result.ToList();
        }, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        VisibleIconCount = filtered.Count;
        FilteredIcons = new ObservableCollection<IconItem>(filtered);
    }

    /// <summary>아이콘 선택 — 다이얼로그 확정 후 닫기</summary>
    [RelayCommand]
    private void SelectIcon(IconItem icon)
    {
        SelectedGlyph = icon.Glyph;
        IsConfirmed = true;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>기본값 복원 — 아이콘 제거 후 닫기</summary>
    [RelayCommand]
    private void ResetToDefault()
    {
        SelectedGlyph = string.Empty;
        IsDefault = true;
        IsConfirmed = true;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>닫기 — 변경 없이 닫기</summary>
    [RelayCommand]
    private void CloseDialog()
    {
        IsConfirmed = false;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Segoe MDL2 Assets 기반 아이콘 목록을 생성합니다.</summary>
    private static IReadOnlyList<IconItem> BuildIconList()
    {
        return
        [
            new("Add",              "\uE710"),
            new("AddFriend",        "\uE8FA"),
            new("Admin",            "\uE7EF"),
            new("Alert",            "\uE7BA"),
            new("Android",          "\uE8CF"),
            new("Attach",           "\uE723"),
            new("Back",             "\uE72B"),
            new("Bluetooth",        "\uE702"),
            new("Brightness",       "\uE706"),
            new("Build",            "\uE8D3"),
            new("Calculator",       "\uE8EF"),
            new("Calendar",         "\uE787"),
            new("Camera",           "\uE722"),
            new("Cancel",           "\uE711"),
            new("Check",            "\uE73E"),
            new("Cloud",            "\uE753"),
            new("Code",             "\uE943"),
            new("ColorPalette",     "\uE790"),
            new("Comment",          "\uE8F2"),
            new("Copy",             "\uE8C8"),
            new("Cut",              "\uE8C6"),
            new("Data",             "\uE9F9"),
            new("Delete",           "\uE74D"),
            new("Document",         "\uE8A5"),
            new("Download",         "\uE896"),
            new("Edit",             "\uE8B5"),
            new("Error",            "\uE814"),
            new("Favorite",         "\uE734"),
            new("Filter",           "\uE71C"),
            new("Flag",             "\uE7C1"),
            new("Folder",           "\uE8B7"),
            new("FolderOpen",       "\uE8DA"),
            new("Forward",          "\uE72A"),
            new("Games",            "\uE7FC"),
            new("Git",              "\uF1D3"),
            new("Globe",            "\uE774"),
            new("Grid",             "\uF0E2"),
            new("Help",             "\uE897"),
            new("Hide",             "\uED1A"),
            new("Home",             "\uE80F"),
            new("Info",             "\uE946"),
            new("Key",              "\uE8D0"),
            new("Left",             "\uE76B"),
            new("Like",             "\uE8E1"),
            new("Link",             "\uE71B"),
            new("List",             "\uE8FD"),
            new("Lock",             "\uE72E"),
            new("Mail",             "\uE715"),
            new("Mobile",           "\uE8EA"),
            new("More",             "\uE712"),
            new("Music",            "\uE8D6"),
            new("Mute",             "\uE74F"),
            new("Network",          "\uEC27"),
            new("Package",          "\uE7B8"),
            new("Paste",            "\uE77F"),
            new("Pause",            "\uE769"),
            new("Phone",            "\uE717"),
            new("Photo",            "\uE8B9"),
            new("Pin",              "\uE718"),
            new("Play",             "\uE768"),
            new("Power",            "\uE7E8"),
            new("Print",            "\uE749"),
            new("Redo",             "\uE7A6"),
            new("Refresh",          "\uE72C"),
            new("Right",            "\uE76C"),
            new("Robot",            "\uE99A"),
            new("Save",             "\uE74E"),
            new("Search",           "\uE721"),
            new("Send",             "\uE724"),
            new("Server",           "\uECC8"),
            new("Settings",         "\uE713"),
            new("Share",            "\uE72D"),
            new("Sort",             "\uE8CB"),
            new("Sound",            "\uE995"),
            new("Star",             "\uE735"),
            new("Stop",             "\uE71A"),
            new("Sync",             "\uE895"),
            new("Tag",              "\uE8EC"),
            new("Terminal",         "\uE756"),
            new("Tools",            "\uE8F1"),
            new("Unpin",            "\uE77A"),
            new("Upload",           "\uE898"),
            new("USB",              "\uED55"),
            new("Video",            "\uE8B2"),
            new("View",             "\uE890"),
            new("Warning",          "\uE7BA"),
            new("Web",              "\uE774"),
            new("Wifi",             "\uE701"),
            new("Windows",          "\uE782"),
            new("ZoomIn",           "\uE8A3"),
            new("ZoomOut",          "\uE71F"),
        ];
    }
}

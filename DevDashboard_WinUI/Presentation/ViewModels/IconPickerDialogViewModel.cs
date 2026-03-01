using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>아이콘 항목 — 이름(검색용), 글리프(유니코드 문자), 태그(동의어 검색용)</summary>
public sealed record IconItem(string Name, string Glyph, IReadOnlyList<string> Tags);

/// <summary>아이콘 선택 다이얼로그 뷰모델 — IconsData.json 기반 아이콘 목록 표시</summary>
public partial class IconPickerDialogViewModel : ObservableObject
{
    private IReadOnlyList<IconItem> _allIcons = [];
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
        _allIcons = await LoadIconsFromJsonAsync();
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

    /// <summary>검색어 기반 필터링 — 이름과 태그 모두 검색</summary>
    private async Task ApplyFilterAsync(CancellationToken cancellationToken)
    {
        var query = SearchText?.Trim() ?? string.Empty;
        var icons = _allIcons;

        var filtered = await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<IconItem> result = icons;
            if (!string.IsNullOrWhiteSpace(query))
            {
                result = result.Where(i =>
                    i.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    i.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)));
            }

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

    /// <summary>Data/IconsData.json에서 아이콘 목록을 비동기로 로드합니다.</summary>
    private static async Task<IReadOnlyList<IconItem>> LoadIconsFromJsonAsync()
    {
        var file = await StorageFile.GetFileFromApplicationUriAsync(
            new Uri("ms-appx:///Data/IconsData.json"));
        var json = await FileIO.ReadTextAsync(file);

        var entries = JsonSerializer.Deserialize<List<IconJsonEntry>>(json, JsonOptions);
        if (entries is null)
            return [];

        var items = new List<IconItem>(entries.Count);
        foreach (var entry in entries)
        {
            // hex 코드("E700")를 글리프 문자로 변환
            if (int.TryParse(entry.Code, System.Globalization.NumberStyles.HexNumber, null, out var codePoint))
            {
                var glyph = char.ConvertFromUtf32(codePoint);
                items.Add(new IconItem(entry.Name, glyph, entry.Tags ?? []));
            }
        }

        return items;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>IconsData.json 역직렬화용 DTO</summary>
    private sealed record IconJsonEntry
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public List<string>? Tags { get; init; }

        [JsonPropertyName("IsSegoeFluentOnly")]
        public bool IsSegoeFluentOnly { get; init; }
    }
}

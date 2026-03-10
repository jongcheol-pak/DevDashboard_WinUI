using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevDashboard.Infrastructure.Persistence;
using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.Views.Dialogs;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>
/// 좌측 사이드바 런처 ViewModel.
/// 등록된 앱 목록 관리 및 실행 기능을 제공합니다.
/// </summary>
public sealed partial class LauncherViewModel : ObservableObject
{
    private readonly LauncherRepository? _repository;

    /// <summary>등록된 런처 항목 목록 (데이터 소스)</summary>
    public ObservableCollection<LauncherItemViewModel> Items { get; } = [];

    /// <summary>UI 표시용 컬렉션 (아이템 + DropPlaceholder 혼합)</summary>
    public ObservableCollection<object> DisplayItems { get; } = [];

    public LauncherViewModel(LauncherRepository? repository)
    {
        _repository = repository;
        LoadItems();
    }

    private void LoadItems()
    {
        if (_repository is null) return;

        try
        {
            var items = _repository.GetAll();
            foreach (var item in items)
                Items.Add(new LauncherItemViewModel(item));
            SyncDisplayItems();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"런처 항목 로드 실패: {ex.Message}");
        }
    }

    /// <summary>Items → DisplayItems 동기화</summary>
    public void SyncDisplayItems()
    {
        DisplayItems.Clear();
        foreach (var item in Items)
            DisplayItems.Add(item);
    }

    /// <summary>설치된 앱 선택 다이얼로그를 표시하고 선택된 앱을 등록합니다.</summary>
    [RelayCommand]
    private async Task AddAsync()
    {
        var dialog = new InstalledAppsDialog();
        var result = await dialog.ShowAsync();

        if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            return;

        var selectedApp = dialog.ResultApp;
        if (selectedApp is null) return;

        var launcherItem = new LauncherItem
        {
            DisplayName = selectedApp.DisplayName,
            ExecutablePath = selectedApp.ExecutablePath,
            IconCachePath = selectedApp.IconPath ?? string.Empty,
            SortOrder = Items.Count
        };

        _repository?.Add(launcherItem);

        var vm = new LauncherItemViewModel(launcherItem);
        await vm.LoadIconAsync();
        Items.Add(vm);
        SyncDisplayItems();
    }

    /// <summary>지정된 항목을 삭제합니다.</summary>
    [RelayCommand]
    private void Delete(LauncherItemViewModel? item)
    {
        if (item is null) return;

        _repository?.Delete(item.Id);
        Items.Remove(item);
        SaveSortOrders();
        SyncDisplayItems();
    }

    /// <summary>드래그&드롭으로 항목을 대상 기준 위/아래로 이동합니다.</summary>
    public void MoveItem(string draggedId, string targetId, bool insertAfter)
    {
        var dragged = Items.FirstOrDefault(x => x.Id == draggedId);
        var target = Items.FirstOrDefault(x => x.Id == targetId);
        if (dragged is null || target is null || dragged == target) return;

        int fromIndex = Items.IndexOf(dragged);
        int toIndex = Items.IndexOf(target);
        if (insertAfter) toIndex++;
        if (fromIndex < toIndex) toIndex--;

        if (fromIndex == toIndex) return;

        Items.Move(fromIndex, toIndex);
        SaveSortOrders();
        SyncDisplayItems();
    }

    private void SaveSortOrders()
    {
        for (int i = 0; i < Items.Count; i++)
            Items[i].SortOrder = i;

        if (_repository is not null)
        {
            var entities = Items.Select(vm => new LauncherItem
            {
                Id = vm.Id,
                SortOrder = vm.SortOrder
            }).ToList();
            _repository.UpdateSortOrders(entities);
        }
    }

    /// <summary>앱을 실행합니다.</summary>
    [RelayCommand]
    private static void Launch(LauncherItemViewModel? item)
    {
        if (item is null) return;
        LaunchApp(item.ExecutablePath, runAsAdmin: false);
    }

    /// <summary>관리자 권한으로 앱을 실행합니다.</summary>
    [RelayCommand]
    private static void LaunchAsAdmin(LauncherItemViewModel? item)
    {
        if (item is null) return;
        LaunchApp(item.ExecutablePath, runAsAdmin: true);
    }

    private static void LaunchApp(string executablePath, bool runAsAdmin)
    {
        try
        {
            var psi = new ProcessStartInfo();

            if (executablePath.StartsWith("shell:AppsFolder", StringComparison.OrdinalIgnoreCase))
            {
                // UWP/MSIX 앱: explorer.exe로 실행
                psi.FileName = "explorer.exe";
                psi.Arguments = executablePath;
            }
            else
            {
                psi.FileName = executablePath;
                psi.UseShellExecute = true;
                if (runAsAdmin)
                    psi.Verb = "runas";
            }

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"앱 실행 실패 ({executablePath}): {ex.Message}");
        }
    }
}

/// <summary>
/// 런처 항목 개별 ViewModel (UI 바인딩용).
/// </summary>
public sealed partial class LauncherItemViewModel : ObservableObject
{
    public string Id { get; }
    public string DisplayName { get; }
    public string ExecutablePath { get; }
    public string IconCachePath { get; }
    public int SortOrder { get; set; }

    [ObservableProperty]
    public partial BitmapImage? IconImage { get; set; }

    public LauncherItemViewModel(LauncherItem item)
    {
        Id = item.Id;
        DisplayName = item.DisplayName;
        ExecutablePath = item.ExecutablePath;
        IconCachePath = item.IconCachePath;
        SortOrder = item.SortOrder;

        // 비동기 아이콘 로딩 시작
        _ = LoadIconAsync();
    }

    internal async Task LoadIconAsync()
    {
        if (string.IsNullOrEmpty(IconCachePath)) return;

        try
        {
            IconImage = await IconCacheService.LoadImageAsync(IconCachePath, 36);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"아이콘 로드 실패 ({DisplayName}): {ex.Message}");
        }
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>
/// 알림 전체 페이지 뷰모델 — 마감 알림을 프로젝트별로 묶어 표시하고, 읽음 처리·작업 이동을 처리합니다.
/// 알림 집계·읽음 상태 영속화는 MainViewModel이 소유하며, 이 뷰모델은 주입된 콜백으로 위임합니다.
/// </summary>
public partial class NotificationPageViewModel : ObservableObject
{
    private readonly IReadOnlyList<Notification> _notifications;
    private readonly Action<Notification> _markRead;
    private readonly Action<IEnumerable<Notification>> _markAllRead;
    private readonly Action<string> _navigateToProject;

    /// <summary>프로젝트별 알림 그룹</summary>
    public ObservableCollection<NotificationProjectGroup> Groups { get; } = [];

    /// <summary>표시할 알림이 하나라도 있는지 여부 (빈 상태 표시용)</summary>
    [ObservableProperty] public partial bool HasNotifications { get; set; }

    /// <param name="notifications">현재 마감 알림 목록(종료일 오름차순)</param>
    /// <param name="readKeys">읽음 처리된 알림 키 집합</param>
    /// <param name="markRead">항목 읽음 처리 콜백(영속화는 MainViewModel)</param>
    /// <param name="markAllRead">전체 읽음 처리 콜백</param>
    /// <param name="navigateToProject">프로젝트 Id로 작업 페이지 이동 콜백</param>
    public NotificationPageViewModel(
        IReadOnlyList<Notification> notifications,
        IReadOnlySet<string> readKeys,
        Action<Notification> markRead,
        Action<IEnumerable<Notification>> markAllRead,
        Action<string> navigateToProject)
    {
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(readKeys);
        ArgumentNullException.ThrowIfNull(markRead);
        ArgumentNullException.ThrowIfNull(markAllRead);
        ArgumentNullException.ThrowIfNull(navigateToProject);

        _notifications = notifications;
        _markRead = markRead;
        _markAllRead = markAllRead;
        _navigateToProject = navigateToProject;

        BuildGroups(readKeys);
    }

    /// <summary>알림을 프로젝트별로 묶어 그룹을 구성합니다(각 그룹 내 종료일 오름차순 유지).</summary>
    private void BuildGroups(IReadOnlySet<string> readKeys)
    {
        Groups.Clear();
        foreach (var group in _notifications.GroupBy(n => n.ProjectId))
        {
            var items = group
                .Select(n => new NotificationItemViewModel(n, readKeys.Contains(NotificationService.BuildKey(n))))
                .ToList();
            Groups.Add(new NotificationProjectGroup(group.Key, group.First().ProjectName, items));
        }

        HasNotifications = _notifications.Count > 0;
    }

    /// <summary>단일 알림을 읽음 처리합니다.</summary>
    [RelayCommand]
    private void MarkRead(NotificationItemViewModel? item)
    {
        if (item is null || item.IsRead) return;
        item.IsRead = true;
        _markRead(item.Model);
    }

    /// <summary>표시 중인 모든 알림을 읽음 처리합니다.</summary>
    [RelayCommand]
    private void MarkAllRead()
    {
        foreach (var group in Groups)
            foreach (var item in group.Items)
                item.IsRead = true;

        _markAllRead(_notifications);
    }

    /// <summary>알림 항목의 프로젝트 작업 페이지로 이동합니다.</summary>
    [RelayCommand]
    private void OpenTask(NotificationItemViewModel? item)
    {
        if (item is null) return;
        _navigateToProject(item.Model.ProjectId);
    }
}

/// <summary>알림 목록의 개별 항목 뷰모델 — 읽음 여부를 UI에 노출합니다.</summary>
public partial class NotificationItemViewModel : ObservableObject
{
    /// <summary>원본 알림(불변 사실)</summary>
    public Notification Model { get; }

    /// <summary>읽음 여부 (안읽음 강조·읽음 흐림 표시용)</summary>
    [ObservableProperty] public partial bool IsRead { get; set; }

    public NotificationItemViewModel(Notification model, bool isRead)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
        IsRead = isRead;
    }

    /// <summary>작업 본문 텍스트(표시용)</summary>
    public string TodoText => Model.TodoText;

    /// <summary>작업 종료(마감)일</summary>
    public DateTime EndDate => Model.EndDate;

    /// <summary>마감 긴급도 상태(임박/오늘/경과 — 배지 색·라벨 매핑용)</summary>
    public DeadlineStatus Status => Model.Status;
}

/// <summary>프로젝트별로 묶은 알림 그룹</summary>
public sealed record NotificationProjectGroup(string ProjectId, string ProjectName, IReadOnlyList<NotificationItemViewModel> Items);

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevDashboard.Infrastructure.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace DevDashboard.Presentation.ViewModels;

/// <summary>
/// 알림 전체 페이지 뷰모델 — 마감 알림을 프로젝트별로 묶어 표시하고, 읽음 토글·작업 이동을 처리합니다.
/// 알림 집계·읽음 상태 영속화는 MainViewModel이 소유하며, 이 뷰모델은 주입된 콜백으로 위임합니다.
/// </summary>
public partial class NotificationPageViewModel : ObservableObject
{
    private readonly IReadOnlyList<Notification> _notifications;
    private readonly Action<Notification> _markRead;
    private readonly Action<Notification> _unmarkRead;
    private readonly Action<IEnumerable<Notification>> _markAllRead;
    private readonly Action<string> _navigateToProject;

    private static readonly string _unreadFormat = LocalizationService.Get("Notification_UnreadFormat");

    /// <summary>프로젝트별 알림 그룹</summary>
    public ObservableCollection<NotificationProjectGroup> Groups { get; } = [];

    /// <summary>표시할 알림이 하나라도 있는지 여부 (빈 상태 표시용)</summary>
    [ObservableProperty] public partial bool HasNotifications { get; set; }

    /// <summary>안읽음 건수</summary>
    [NotifyPropertyChangedFor(nameof(HasUnread))]
    [NotifyPropertyChangedFor(nameof(UnreadCaption))]
    [ObservableProperty] public partial int UnreadCount { get; set; }

    /// <summary>안읽음이 하나라도 있는지 여부 ("모두 읽음" 버튼 표시 조건 — 시안은 안읽음이 없으면 숨긴다)</summary>
    public bool HasUnread => UnreadCount > 0;

    /// <summary>헤더 pill 문구 ("읽지 않음 N건")</summary>
    public string UnreadCaption => string.Format(_unreadFormat, UnreadCount);

    /// <param name="notifications">현재 마감 알림 목록(종료일 오름차순)</param>
    /// <param name="readKeys">읽음 처리된 알림 키 집합</param>
    /// <param name="markRead">항목 읽음 처리 콜백(영속화는 MainViewModel)</param>
    /// <param name="unmarkRead">항목 읽음 해제 콜백(영속화는 MainViewModel)</param>
    /// <param name="markAllRead">전체 읽음 처리 콜백</param>
    /// <param name="navigateToProject">프로젝트 Id로 작업 페이지 이동 콜백</param>
    public NotificationPageViewModel(
        IReadOnlyList<Notification> notifications,
        IReadOnlySet<string> readKeys,
        Action<Notification> markRead,
        Action<Notification> unmarkRead,
        Action<IEnumerable<Notification>> markAllRead,
        Action<string> navigateToProject)
    {
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(readKeys);
        ArgumentNullException.ThrowIfNull(markRead);
        ArgumentNullException.ThrowIfNull(unmarkRead);
        ArgumentNullException.ThrowIfNull(markAllRead);
        ArgumentNullException.ThrowIfNull(navigateToProject);

        _notifications = notifications;
        _markRead = markRead;
        _unmarkRead = unmarkRead;
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
                .Select(n => CreateItem(n, readKeys.Contains(NotificationService.BuildKey(n))))
                .ToList();
            Groups.Add(new NotificationProjectGroup(group.Key, group.First().ProjectName, items));
        }

        HasNotifications = _notifications.Count > 0;
        RecountUnread();
    }

    /// <summary>항목 뷰모델을 만들고 읽음 상태 변경을 안읽음 집계에 연결합니다.</summary>
    private NotificationItemViewModel CreateItem(Notification model, bool isRead)
    {
        var item = new NotificationItemViewModel(model, isRead);
        item.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NotificationItemViewModel.IsRead))
                RecountUnread();
        };
        return item;
    }

    /// <summary>표시 중인 항목에서 안읽음 건수를 다시 셉니다.</summary>
    private void RecountUnread()
        => UnreadCount = Groups.Sum(g => g.Items.Count(i => !i.IsRead));

    /// <summary>단일 알림의 읽음 여부를 토글합니다(시안의 읽음 버튼은 양방향).</summary>
    [RelayCommand]
    private void ToggleRead(NotificationItemViewModel? item)
    {
        if (item is null) return;

        if (item.IsRead)
        {
            item.IsRead = false;
            _unmarkRead(item.Model);
        }
        else
        {
            item.IsRead = true;
            _markRead(item.Model);
        }
    }

    /// <summary>표시 중인 모든 알림을 읽음 처리합니다.</summary>
    [RelayCommand]
    private void MarkAllRead()
    {
        foreach (var group in Groups)
            foreach (var item in group.Items)
                item.IsRead = true;

        _markAllRead(_notifications);

        // 항목마다 IsRead 알림이 오면서 RecountUnread가 항목 수만큼 전체 순회를 반복하므로,
        // 결과를 아는 이 자리에서 직접 확정한다.
        UnreadCount = 0;
    }

    /// <summary>알림 항목의 프로젝트 작업 페이지로 이동합니다(시안대로 이동 시 읽음 처리도 함께).</summary>
    [RelayCommand]
    private void OpenTask(NotificationItemViewModel? item)
    {
        if (item is null) return;

        if (!item.IsRead)
        {
            item.IsRead = true;
            _markRead(item.Model);
        }

        _navigateToProject(item.Model.ProjectId);
    }
}

/// <summary>
/// 알림 목록의 개별 항목 뷰모델 — 시안 행이 필요로 하는 표시값(문구·색·투명도)을 모두 노출합니다.
/// 색을 코드비하인드 정적 헬퍼가 아니라 여기에 두는 이유: 알림 팝업은 Window(MainWindow.xaml) 안에 있어
/// x:Bind 함수 바인딩을 쓸 수 없고(AGENTS.md 함정 9), 페이지와 팝업이 같은 값을 써야 하기 때문이다.
/// </summary>
public partial class NotificationItemViewModel : ObservableObject
{
    // 마감상태 톤 — 시안(notifVals의 toneOf)이 정본이다. Palette.xaml 값과 의도적으로 다르니
    // 팔레트 값으로 되돌리지 말 것(TaskPage.PriorityBrush와 같은 규약).
    //   경과 글자 #F0716A / 배경 같은 색의 저채도(알파 0x24 — 시안 .14 근사)
    //   오늘 글자 #E8925A / 배경 알파 0x28 (시안 .16)
    //   임박 글자 #7AB5EC / 배경은 시안이 글자보다 진한 #5AA3E8 기준이라 베이스가 다르다
    private static readonly SolidColorBrush _overdueBrush = new(ColorHelper.FromArgb(0xFF, 0xF0, 0x71, 0x6A));
    private static readonly SolidColorBrush _dueTodayBrush = new(ColorHelper.FromArgb(0xFF, 0xE8, 0x92, 0x5A));
    private static readonly SolidColorBrush _imminentBrush = new(ColorHelper.FromArgb(0xFF, 0x7A, 0xB5, 0xEC));
    private static readonly SolidColorBrush _overdueSoftBrush = new(ColorHelper.FromArgb(0x24, 0xF0, 0x71, 0x6A));
    private static readonly SolidColorBrush _dueTodaySoftBrush = new(ColorHelper.FromArgb(0x28, 0xE8, 0x92, 0x5A));
    private static readonly SolidColorBrush _imminentSoftBrush = new(ColorHelper.FromArgb(0x28, 0x5A, 0xA3, 0xE8));

    // 읽음 버튼 색 — 안읽음 회색(#8A8890)/테두리 #3A3A41, 읽음 초록(#5DB463)/테두리 #2E2E34 (시안 readBtnStyle)
    private static readonly SolidColorBrush _readButtonIdleBrush = new(ColorHelper.FromArgb(0xFF, 0x8A, 0x88, 0x90));
    private static readonly SolidColorBrush _readButtonDoneBrush = new(ColorHelper.FromArgb(0xFF, 0x5D, 0xB4, 0x63));
    private static readonly SolidColorBrush _readButtonIdleBorderBrush = new(ColorHelper.FromArgb(0xFF, 0x3A, 0x3A, 0x41));
    private static readonly SolidColorBrush _readButtonDoneBorderBrush = new(ColorHelper.FromArgb(0xFF, 0x2E, 0x2E, 0x34));

    // 행 상자·부제 — 시안 rowStyle(#1F1F24 / 테두리 #27272D)과 부제 글자(#98959D).
    // 팔레트에 같은 값이 없어 시안 값을 그대로 쓴다(팔레트로 근사 대체하지 않는다).
    private static readonly SolidColorBrush _rowBackgroundBrush = new(ColorHelper.FromArgb(0xFF, 0x1F, 0x1F, 0x24));
    private static readonly SolidColorBrush _rowBorderBrush = new(ColorHelper.FromArgb(0xFF, 0x27, 0x27, 0x2D));
    private static readonly SolidColorBrush _subCaptionBrush = new(ColorHelper.FromArgb(0xFF, 0x98, 0x95, 0x9D));

    private static readonly string _duePrefix = LocalizationService.Get("Notification_DuePrefix");
    private static readonly string _categoryNone = LocalizationService.Get("TaskCategory_None");
    private static readonly string _badgeOverdue = LocalizationService.Get("Notification_Badge_Overdue");
    private static readonly string _badgeDueToday = LocalizationService.Get("Notification_Badge_DueToday");
    private static readonly string _badgeImminent = LocalizationService.Get("Notification_Badge_Imminent");
    private static readonly string _markReadTooltip = LocalizationService.Get("Notification_MarkRead_Tooltip");
    private static readonly string _markUnreadTooltip = LocalizationService.Get("Notification_MarkUnread_Tooltip");

    /// <summary>원본 알림(불변 사실)</summary>
    public Notification Model { get; }

    /// <summary>읽음 여부 (행 투명도·읽음 버튼 색·툴팁이 여기에 딸린다)</summary>
    [NotifyPropertyChangedFor(nameof(RowOpacity))]
    [NotifyPropertyChangedFor(nameof(ReadButtonForeground))]
    [NotifyPropertyChangedFor(nameof(ReadButtonBorder))]
    [NotifyPropertyChangedFor(nameof(ReadToggleTooltip))]
    [ObservableProperty] public partial bool IsRead { get; set; }

    public NotificationItemViewModel(Notification model, bool isRead)
    {
        ArgumentNullException.ThrowIfNull(model);
        Model = model;
        IsRead = isRead;
    }

    /// <summary>작업 본문 텍스트(표시용)</summary>
    public string TodoText => Model.TodoText;

    /// <summary>프로젝트 표시 이름 (팝업 부제용)</summary>
    public string ProjectName => Model.ProjectName;

    /// <summary>페이지 행 부제 ("카테고리 · 종료일 yyyy-MM-dd")</summary>
    public string SubCaption => $"{CategoryLabel} · {_duePrefix} {Model.EndDate:yyyy-MM-dd}";

    /// <summary>팝업 행 부제 ("프로젝트 · 카테고리 · 종료일 yyyy-MM-dd" — 팝업은 프로젝트별 그룹이 없다)</summary>
    public string PopupSubCaption => $"{ProjectName} · {SubCaption}";

    /// <summary>마감상태 배지 문구 ("48일 경과" / "오늘 마감" / "D-2")</summary>
    public string BadgeText
    {
        get
        {
            // 시안 공식: d = 오늘 − 종료일(일). 양수면 경과, 0이면 오늘 마감, 음수면 남은 일수를 D-n으로.
            var days = (DateTime.Today - Model.EndDate.Date).Days;
            return days > 0 ? string.Format(_badgeOverdue, days)
                : days == 0 ? _badgeDueToday
                : string.Format(_badgeImminent, days);
        }
    }

    /// <summary>배지·아이콘 글자 색 (마감상태 톤)</summary>
    public Brush BadgeForeground => Model.Status switch
    {
        DeadlineStatus.Overdue => _overdueBrush,
        DeadlineStatus.DueToday => _dueTodayBrush,
        _ => _imminentBrush,
    };

    /// <summary>배지·아이콘 배경 색 (같은 톤의 저채도 변형)</summary>
    public Brush BadgeBackground => Model.Status switch
    {
        DeadlineStatus.Overdue => _overdueSoftBrush,
        DeadlineStatus.DueToday => _dueTodaySoftBrush,
        _ => _imminentSoftBrush,
    };

    /// <summary>읽음 버튼 글자 색 (읽음이면 초록)</summary>
    public Brush ReadButtonForeground => IsRead ? _readButtonDoneBrush : _readButtonIdleBrush;

    /// <summary>읽음 버튼 테두리 색 (읽음이면 흐리게)</summary>
    public Brush ReadButtonBorder => IsRead ? _readButtonDoneBorderBrush : _readButtonIdleBorderBrush;

    /// <summary>읽음 버튼 툴팁 (토글 방향을 알린다)</summary>
    public string ReadToggleTooltip => IsRead ? _markUnreadTooltip : _markReadTooltip;

    /// <summary>읽은 항목은 흐리게 표시 (읽음 0.5 / 안읽음 1.0)</summary>
    public double RowOpacity => IsRead ? 0.5 : 1.0;

    /// <summary>페이지 행 배경 (시안 rowStyle)</summary>
    public Brush RowBackground => _rowBackgroundBrush;

    /// <summary>페이지 행 기본 테두리 (마우스 오버 시 코드비하인드가 밝은 색으로 덮었다가 되돌린다)</summary>
    public Brush RowBorder => _rowBorderBrush;

    /// <summary>부제 글자 색 (페이지·팝업 공통)</summary>
    public Brush SubCaptionForeground => _subCaptionBrush;

    /// <summary>카테고리 표시명 (빈 카테고리는 "미분류" — TaskPage 그룹 헤더와 같은 규약)</summary>
    private string CategoryLabel
        => string.IsNullOrWhiteSpace(Model.TodoCategory) ? _categoryNone : Model.TodoCategory;
}

/// <summary>프로젝트별로 묶은 알림 그룹</summary>
public sealed record NotificationProjectGroup(string ProjectId, string ProjectName, IReadOnlyList<NotificationItemViewModel> Items)
{
    /// <summary>그룹 헤더에 표시할 알림 건수</summary>
    public int Count => Items.Count;
}

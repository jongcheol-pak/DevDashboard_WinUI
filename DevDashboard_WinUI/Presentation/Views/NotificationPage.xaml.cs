using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace DevDashboard.Presentation.Views;

/// <summary>알림 전체 페이지 — 헤더 벨의 "모든 알림 보기"에서 표시됩니다.</summary>
public sealed partial class NotificationPage : UserControl
{
    /// <summary>페이지 뷰모델 (x:Bind 대상)</summary>
    public NotificationPageViewModel Vm { get; }

    public NotificationPage(NotificationPageViewModel vm)
    {
        Vm = vm;
        InitializeComponent();
        DataContext = vm;
    }

    // ===== 마감상태 색 (PRD §3 팔레트: 경과 코랄 #f0716a / 오늘 앰버 #e8b45a / 임박 블루 #5aa3e8) =====

    private static readonly SolidColorBrush _overdueBrush = new(ColorHelper.FromArgb(0xFF, 0xF0, 0x71, 0x6A));
    private static readonly SolidColorBrush _dueTodayBrush = new(ColorHelper.FromArgb(0xFF, 0xE8, 0xB4, 0x5A));
    private static readonly SolidColorBrush _imminentBrush = new(ColorHelper.FromArgb(0xFF, 0x5A, 0xA3, 0xE8));

    // 헤더·툴팁 (x:Bind 정적 참조)
    public static string PageTitle { get; } = LocalizationService.Get("Notification_Title");
    public static string BackText { get; } = LocalizationService.Get("Notification_Back");
    public static string MarkAllText { get; } = LocalizationService.Get("Notification_MarkAll");
    public static string EmptyText { get; } = LocalizationService.Get("Notification_Empty");
    public static string MarkReadTooltip { get; } = LocalizationService.Get("Notification_MarkRead_Tooltip");

    private static readonly string _duePrefix = LocalizationService.Get("Notification_DuePrefix");

    // ===== x:Bind 정적 헬퍼 (Converter 미적용 — 직접 타입 반환) =====

    /// <summary>마감상태 배지 색 브러시 (임박/오늘/경과)</summary>
    public static Brush DeadlineBrush(DeadlineStatus status) => status switch
    {
        DeadlineStatus.Overdue => _overdueBrush,
        DeadlineStatus.DueToday => _dueTodayBrush,
        _ => _imminentBrush,
    };

    /// <summary>마감상태 배지 라벨 (임박/오늘 마감/경과)</summary>
    public static string DeadlineLabel(DeadlineStatus status)
        => LocalizationService.Get($"Notification_Deadline_{status}");

    /// <summary>종료일 표시 ("마감 yyyy-MM-dd")</summary>
    public static string FormatDue(DateTime date) => $"{_duePrefix} {date:yyyy-MM-dd}";

    /// <summary>안읽음 표시 점의 표시 여부 (안읽음일 때만 표시)</summary>
    public static Visibility UnreadDotVisibility(bool isRead)
        => isRead ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>읽음 항목은 흐리게 표시 (읽음 0.5 / 안읽음 1.0)</summary>
    public static double ReadOpacity(bool isRead) => isRead ? 0.5 : 1.0;

    // ===== 네비게이션·상호작용 =====

    private void Back_Click(object sender, RoutedEventArgs e)
        => (App.MainWindow as MainWindow)?.ShowDashboard();

    /// <summary>알림 항목 클릭 — 해당 프로젝트 작업 페이지로 이동합니다.</summary>
    private void Item_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: NotificationItemViewModel item })
            Vm.OpenTaskCommand.Execute(item);
    }

    /// <summary>항목 읽음 처리 버튼.</summary>
    private void MarkRead_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: NotificationItemViewModel item })
            Vm.MarkReadCommand.Execute(item);
    }
}

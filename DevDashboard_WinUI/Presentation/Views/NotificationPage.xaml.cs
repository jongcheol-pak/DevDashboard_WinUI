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

    // 행 마우스 오버 테두리 #3D3D45 (시안 style-hover). 기본 테두리는 항목 뷰모델의 RowBorder가 갖는다.
    private static readonly SolidColorBrush _rowHoverBorderBrush = new(ColorHelper.FromArgb(0xFF, 0x3D, 0x3D, 0x45));

    // 화면 고정 문구 (x:Bind 정적 참조). 항목별로 달라지는 값은 전부 NotificationItemViewModel이 갖는다.
    public static string PageTitle { get; } = LocalizationService.Get("Notification_Title");
    public static string BackText { get; } = LocalizationService.Get("Notification_Back");
    public static string MarkAllText { get; } = LocalizationService.Get("Notification_MarkAll");
    public static string EmptyText { get; } = LocalizationService.Get("Notification_Empty");
    public static string EmptyHintText { get; } = LocalizationService.Get("Notification_EmptyHint");

    // ===== 네비게이션·상호작용 =====

    private void Back_Click(object sender, RoutedEventArgs e)
        => (App.MainWindow as MainWindow)?.ShowDashboard();

    /// <summary>알림 항목 클릭 — 해당 프로젝트 작업 페이지로 이동합니다.</summary>
    private void Item_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: NotificationItemViewModel item })
            Vm.OpenTaskCommand.Execute(item);
    }

    /// <summary>항목 읽음 토글 버튼(읽음 ↔ 안읽음 양방향 — 시안 readBtn).</summary>
    private void ReadToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: NotificationItemViewModel item })
            Vm.ToggleReadCommand.Execute(item);
    }

    /// <summary>행에 마우스를 올리면 테두리를 밝힌다(시안 style-hover).</summary>
    private void Row_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
            border.BorderBrush = _rowHoverBorderBrush;
    }

    /// <summary>행에서 마우스가 벗어나면 기본 테두리로 되돌린다.
    /// ClearValue를 쓰지 않는 이유: XAML의 `{x:Bind RowBorder}`는 기본 OneTime이라 로컬 값을 한 번 대입하는
    /// 코드로 컴파일된다 — 그 값을 지우면 바인딩이 되살아나는 게 아니라 Border의 기본값(테두리 없음)이 된다.</summary>
    private void Row_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border { Tag: NotificationItemViewModel item } border)
            border.BorderBrush = item.RowBorder;
    }
}

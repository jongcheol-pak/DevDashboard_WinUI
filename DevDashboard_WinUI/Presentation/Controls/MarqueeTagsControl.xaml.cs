using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System.Numerics;
using Windows.Foundation;

namespace DevDashboard.Presentation.Controls;

/// <summary>
/// 태그 목록이 카드 너비를 초과할 때 좌→우 마키(Marquee) 스크롤 애니메이션을 표시하는 컨트롤.
/// 애니메이션은 UIElement.Translation + ScalarKeyFrameAnimation으로 Compositor 스레드에서
/// 독립 실행되어 UI 스레드 부하가 없습니다.
/// IsAnimationEnabled = false이거나 태그가 뷰포트 너비 안에 들어올 때는 정적으로 표시됩니다.
/// </summary>
public sealed partial class MarqueeTagsControl : UserControl
{
    // ─── 필드 ───────────────────────────────────────────────────────────────

    /// <summary>중복 Refresh 큐잉 방지 플래그</summary>
    private bool _refreshPending;

    /// <summary>Composition Visual 캐시 — GetElementVisual P/Invoke 반복 호출 방지</summary>
    private Visual? _trackVisual;

    // ─── DependencyProperty: Tags ───────────────────────────────────────────

    public static readonly DependencyProperty TagsProperty =
        DependencyProperty.Register(
            nameof(Tags),
            typeof(IReadOnlyList<string>),
            typeof(MarqueeTagsControl),
            new PropertyMetadata(null, OnTagsChanged));

    /// <summary>표시할 태그 문자열 목록</summary>
    public IReadOnlyList<string>? Tags
    {
        get => (IReadOnlyList<string>?)GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    // ─── DependencyProperty: IsAnimationEnabled ─────────────────────────────

    public static readonly DependencyProperty IsAnimationEnabledProperty =
        DependencyProperty.Register(
            nameof(IsAnimationEnabled),
            typeof(bool),
            typeof(MarqueeTagsControl),
            new PropertyMetadata(true, OnIsAnimationEnabledChanged));

    /// <summary>마키 애니메이션 활성화 여부</summary>
    public bool IsAnimationEnabled
    {
        get => (bool)GetValue(IsAnimationEnabledProperty);
        set => SetValue(IsAnimationEnabledProperty, value);
    }

    // ─── DependencyProperty: MaxTagLength ────────────────────────────────────

    public static readonly DependencyProperty MaxTagLengthProperty =
        DependencyProperty.Register(
            nameof(MaxTagLength),
            typeof(int),
            typeof(MarqueeTagsControl),
            new PropertyMetadata(0, OnMaxTagLengthChanged));

    /// <summary>태그 텍스트 최대 표시 길이. 0이면 제한 없음.</summary>
    public int MaxTagLength
    {
        get => (int)GetValue(MaxTagLengthProperty);
        set => SetValue(MaxTagLengthProperty, value);
    }

    // ─── DP 변경 콜백 ───────────────────────────────────────────────────────

    private static void OnTagsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarqueeTagsControl)d).Refresh();

    private static void OnIsAnimationEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarqueeTagsControl)d).Refresh();

    private static void OnMaxTagLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarqueeTagsControl)d).Refresh();

    // ─── 헬퍼 ────────────────────────────────────────────────────────────────

    /// <summary>MaxTagLength 설정에 따라 잘린 태그 목록을 반환합니다. 0이면 원본 반환.</summary>
    private IReadOnlyList<string>? GetDisplayTags()
    {
        if (Tags is null) return null;
        int max = MaxTagLength;
        if (max <= 0) return Tags;
        return Tags.Select(t => t.Length > max ? t[..max] + "\u2026" : t).ToList();
    }

    // ─── 생성자 ─────────────────────────────────────────────────────────────

    public MarqueeTagsControl()
    {
        InitializeComponent();
        // SizeChanged는 Loaded보다 먼저 발생할 수 있으므로 생성자에서 구독한다.
        // (ItemsRepeater 요소 재활용 시 Loaded 전에 레이아웃이 실행되어 SizeChanged를 놓치는 문제 방지)
        Viewport.SizeChanged += OnViewportSizeChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    // ─── 로드/언로드 ────────────────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnsureTrackVisual();
        Refresh();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // _refreshPending 초기화: 빠른 스크롤로 카드가 재활용될 때
        // 콜백이 실행되지 못한 채 true로 남아 있으면 재진입 시 애니메이션이 시작되지 않음
        _refreshPending = false;
        StopMarquee();
        _trackVisual = null;
    }

    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs e)
    {
        Refresh();
    }

    // ─── Visual 초기화 ────────────────────────────────────────────────────

    /// <summary>Translation 속성 활성화 + Visual 캐시. 재활용 시에도 안전하게 재설정.</summary>
    private void EnsureTrackVisual()
    {
        ElementCompositionPreview.SetIsTranslationEnabled(Track, true);
        _trackVisual = ElementCompositionPreview.GetElementVisual(Track);
    }

    // ─── 핵심 로직 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 태그 또는 설정 변경 시 마키 상태를 재계산합니다.
    /// _refreshPending 플래그로 중복 Low 콜백을 방지합니다.
    /// </summary>
    private void Refresh()
    {
        if (!IsLoaded) return;

        // 재활용된 요소는 Loaded 없이 바인딩만 변경될 수 있으므로 Visual을 보장
        EnsureTrackVisual();

        // 애니메이션 즉시 중단 + 아이템 소스 최신 값으로 업데이트
        StopMarquee();
        Viewport.Clip = null; // 클립 초기화 — 측정 후 필요 시에만 재적용
        Rep1.ItemsSource = GetDisplayTags();
        Rep2.ItemsSource = null;
        OverflowBadge.Visibility = Visibility.Collapsed;
        Spacer.Visibility = Rep2.Visibility = Visibility.Collapsed;

        // 이미 대기 중인 측정 콜백이 있으면 추가 큐잉 생략
        // (이미 큐잉된 콜백이 실행될 때 최신 Tags/IsAnimationEnabled를 읽음)
        if (_refreshPending) return;
        _refreshPending = true;

        // Low 우선순위 = Normal 레이아웃 패스 완료 후 실행
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            _refreshPending = false;
            if (!IsLoaded) return;

            double viewportWidth = Viewport.ActualWidth;
            if (viewportWidth <= 0) return;

            // 무제한 너비로 측정하여 자연 콘텐츠 너비를 구합니다.
            Rep1.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double contentWidth = Rep1.DesiredSize.Width;

            if (Tags is not { Count: > 0 } || contentWidth <= viewportWidth)
                return; // 태그 없거나 뷰포트에 모두 수용 → 추가 처리 불필요

            if (IsAnimationEnabled)
            {
                // 마키 모드: 클립 적용 + Rep2·Spacer 활성화 후 애니메이션 시작
                Viewport.Clip = new RectangleGeometry
                {
                    Rect = new Rect(0, 0, viewportWidth, Viewport.ActualHeight)
                };
                Rep2.ItemsSource = GetDisplayTags();
                Spacer.Visibility = Rep2.Visibility = Visibility.Visible;
                StartMarquee(contentWidth + 30.0); // +30 = Spacer Width
            }
            else
            {
                // 비-애니메이션 overflow: 뷰포트에 들어가는 태그만 표시 + 나머지는 +N 배지
                TrimTagsToFit(viewportWidth);
            }
        });
    }

    /// <summary>
    /// 비-애니메이션 모드에서 뷰포트에 들어가는 태그만 표시하고, 나머지는 +N 배지로 표시합니다.
    /// 개별 태그 너비를 측정하여 누적 합산으로 뷰포트 초과 지점을 결정합니다.
    /// </summary>
    private void TrimTagsToFit(double viewportWidth)
    {
        var displayTags = GetDisplayTags();
        if (displayTags is null || displayTags.Count == 0) return;

        // +N 배지 너비 측정
        OverflowBadge.Visibility = Visibility.Visible;
        OverflowText.Text = $"+{displayTags.Count}";
        OverflowBadge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double badgeWidth = OverflowBadge.DesiredSize.Width + 4; // 4 = Margin left

        double availableWidth = viewportWidth - badgeWidth;
        double accWidth = 0;
        int visibleCount = 0;

        // 태그 DataTemplate 사양: Border Padding="8,4" MinWidth="30" + StackLayout Spacing="4"
        const double tagPaddingH = 16; // 8 + 8
        const double tagSpacing = 4;
        const double tagMinWidth = 30;

        var measureBlock = new TextBlock { FontSize = 11 };

        for (int i = 0; i < displayTags.Count; i++)
        {
            measureBlock.Text = displayTags[i];
            measureBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double tagWidth = Math.Max(measureBlock.DesiredSize.Width + tagPaddingH, tagMinWidth);

            if (i > 0) accWidth += tagSpacing;
            accWidth += tagWidth;

            if (accWidth > availableWidth) break;
            visibleCount++;
        }

        int remaining = displayTags.Count - visibleCount;
        if (remaining > 0)
        {
            Rep1.ItemsSource = displayTags.Take(visibleCount).ToList();
            OverflowText.Text = $"+{remaining}";
        }
        else
        {
            OverflowBadge.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Compositor 스레드에서 독립 실행되는 ScalarKeyFrameAnimation으로 마키를 시작합니다.
    /// UI 스레드 부하 없이 60fps 이상 부드러운 스크롤이 가능합니다.
    /// </summary>
    private void StartMarquee(double cycleWidth)
    {
        if (_trackVisual is null) return;

        var compositor = _trackVisual.Compositor;
        var linear = compositor.CreateLinearEasingFunction();

        var animation = compositor.CreateScalarKeyFrameAnimation();
        animation.InsertKeyFrame(0f, 0f, linear);
        animation.InsertKeyFrame(1f, (float)-cycleWidth, linear);
        // 속도: 50px/s, 최소 3초
        animation.Duration = TimeSpan.FromSeconds(Math.Max(3.0, cycleWidth / 50.0));
        animation.IterationBehavior = AnimationIterationBehavior.Forever;

        _trackVisual.StartAnimation("Translation.X", animation);
    }

    /// <summary>마키 애니메이션을 정지하고 초기 위치로 복원합니다.</summary>
    private void StopMarquee()
    {
        if (_trackVisual is null) return;

        _trackVisual.StopAnimation("Translation.X");
        // Compositor 레벨 Translation을 직접 리셋 (UI 스레드와 Compositor 스레드 간 동기화 지연 방지)
        _trackVisual.Properties.InsertVector3("Translation", Vector3.Zero);
        Track.Translation = Vector3.Zero;
        Viewport.Clip = null;
    }
}

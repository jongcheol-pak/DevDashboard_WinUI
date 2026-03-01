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

    /// <summary>Clip 재사용 — SizeChanged마다 새 객체 생성 방지</summary>
    private RectangleGeometry? _viewportClip;

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

    // ─── DP 변경 콜백 ───────────────────────────────────────────────────────

    private static void OnTagsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarqueeTagsControl)d).Refresh();

    private static void OnIsAnimationEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MarqueeTagsControl)d).Refresh();

    // ─── 생성자 ─────────────────────────────────────────────────────────────

    public MarqueeTagsControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    // ─── 로드/언로드 ────────────────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Translation 속성 활성화 — Compositor 스레드 독립 애니메이션 사용을 위해 필수
        ElementCompositionPreview.SetIsTranslationEnabled(Track, true);
        _trackVisual = ElementCompositionPreview.GetElementVisual(Track);

        Viewport.SizeChanged += OnViewportSizeChanged;
        UpdateViewportClip();
        Refresh();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Viewport.SizeChanged -= OnViewportSizeChanged;
        // _refreshPending 초기화: 빠른 스크롤로 카드가 재활용될 때
        // 콜백이 실행되지 못한 채 true로 남아 있으면 재진입 시 애니메이션이 시작되지 않음
        _refreshPending = false;
        StopMarquee();
        _trackVisual = null;
    }

    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateViewportClip();
        Refresh();
    }

    // ─── Clip 갱신 ──────────────────────────────────────────────────────────

    /// <summary>RectangleGeometry를 재사용하여 GC 압력 최소화</summary>
    private void UpdateViewportClip()
    {
        if (_viewportClip is null)
        {
            _viewportClip = new RectangleGeometry();
            Viewport.Clip = _viewportClip;
        }
        _viewportClip.Rect = new Rect(0, 0, Viewport.ActualWidth, 26);
    }

    // ─── 핵심 로직 ──────────────────────────────────────────────────────────

    /// <summary>
    /// 태그 또는 설정 변경 시 마키 상태를 재계산합니다.
    /// _refreshPending 플래그로 중복 Low 콜백을 방지합니다.
    /// </summary>
    private void Refresh()
    {
        if (!IsLoaded) return;

        // 애니메이션 즉시 중단 + 아이템 소스 최신 값으로 업데이트
        StopMarquee();
        Rep1.ItemsSource = Tags;
        Rep2.ItemsSource = null;
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

            // 무제한 너비로 측정하여 자연 콘텐츠 너비를 구합니다.
            Rep1.Measure(new Size(double.PositiveInfinity, 26));
            double contentWidth = Rep1.DesiredSize.Width;
            double viewportWidth = Viewport.ActualWidth;

            if (IsAnimationEnabled && Tags is { Count: > 0 } && contentWidth > viewportWidth)
            {
                // 마키 모드: Rep2·Spacer 활성화 후 애니메이션 시작
                Rep2.ItemsSource = Tags;
                Spacer.Visibility = Rep2.Visibility = Visibility.Visible;
                StartMarquee(contentWidth + 30.0); // +30 = Spacer Width
            }
            // else: 정적 모드 — Rep2·Spacer는 이미 Collapsed 상태
        });
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
        // Translation 초기화: UIElement 속성 직접 리셋
        Track.Translation = Vector3.Zero;
    }
}

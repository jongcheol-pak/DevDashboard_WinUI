using DevDashboard.Infrastructure.Services;
using DevDashboard.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace DevDashboard.Presentation.Views.Dialogs;

public sealed partial class GitStatusDialog : ContentDialog
{
    private readonly ProjectCardViewModel _card;
    private readonly CancellationTokenSource _cts = new();

    // 아바타 색 — DataTemplate의 x:Bind 함수 바인딩은 static만 호출할 수 있는데 브러시는 인스턴스
    // 리소스에서만 꺼낼 수 있어, 생성자에서 한 번 static 필드에 담아 둔다.
    private static Brush[]? _avatarBrushes;

    public GitStatusDialog(ProjectCardViewModel card)
    {
        _card = card;
        InitializeComponent();

        _avatarBrushes ??=
        [
            (Brush)Resources["Avatar1Brush"],
            (Brush)Resources["Avatar2Brush"],
            (Brush)Resources["Avatar3Brush"],
            (Brush)Resources["Avatar4Brush"],
        ];

        // 프로젝트명을 포함한 동적 타이틀 설정
        Title = string.Format(LocalizationService.Get("GitStatusDialog_TitleFormat"), card.Name);
        CloseButtonText = LocalizationService.Get("Dialog_Close");

        Closing += (_, _) =>
        {
            _cts.Cancel();
            _cts.Dispose();
        };
    }

    internal new async Task<ContentDialogResult> ShowAsync()
    {
        XamlRoot = App.MainWindow?.Content?.XamlRoot;
        return await base.ShowAsync();
    }

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        try
        {
            // 뷰 상태를 여기서 초기화하지 않는다 — 이 다이얼로그는 열 때마다 새로 생성되고
            // (DashboardView.xaml.cs:186) 두 뷰 모두 XAML에서 Collapsed로 시작한다.
            BackText.Text = LocalizationService.Get("GitDetail_Back");

            string? error;
            try
            {
                error = await _card.LoadGitStatusAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            LoadingPanel.Visibility = Visibility.Collapsed;

            if (error is not null)
            {
                ErrorBar.Message = error;
                ErrorBar.IsOpen = true;
                return;
            }

            BranchText.Text = _card.GitBranch;
            CommitList.ItemsSource = _card.GitCommits;

            NoCommitsText.Visibility = _card.GitCommits.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            ListArea.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            ErrorBar.Message = string.Format(LocalizationService.Get("UnexpectedError"), ex.Message);
            ErrorBar.IsOpen = true;
        }
    }

    // --- 뷰 전환 ---

    private void ShowList()
    {
        DetailArea.Visibility = Visibility.Collapsed;
        ListArea.Visibility = Visibility.Visible;
    }

    private void ShowDetail(GitCommit commit)
    {
        DetailAvatar.Fill = AvatarBrush(commit.AuthorEmail);
        DetailAvatarInitial.Text = AvatarInitial(commit.AuthorName);
        DetailAuthorText.Text = commit.AuthorName;

        var date = commit.Date.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
        DetailMetaText.Text = string.IsNullOrWhiteSpace(commit.AuthorEmail)
            ? date
            : $"{date} · {commit.AuthorEmail}";

        DetailMessageText.Text = commit.Message.TrimEnd();

        ListArea.Visibility = Visibility.Collapsed;
        DetailArea.Visibility = Visibility.Visible;
    }

    private void CommitRow_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: GitCommit commit })
            ShowDetail(commit);
    }

    private void BackToList_Click(object sender, RoutedEventArgs e) => ShowList();

    private void CommitRow_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
            border.Background = (Brush)Resources["RowHoverBrush"];
    }

    private void CommitRow_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        // null이 아니라 Transparent로 되돌린다 — 배경이 null이면 행이 포인터 입력을 받지 못해
        // Tapped(상세 전환)가 죽는다
        if (sender is Border border)
            border.Background = (Brush)Resources["RowRestBrush"];
    }

    // --- 표시 변환 (x:Bind 함수 바인딩은 Converter도 ThemeResource도 받지 못해 코드에서 직접 반환한다) ---

    /// <summary>커밋 시각을 "방금 전 / N분 전 / N시간 전 / N일 전"으로, 30일이 넘으면 날짜로 표시합니다.</summary>
    public static string RelativeTime(DateTimeOffset date)
    {
        var elapsed = DateTimeOffset.Now - date;

        // 시계 오차·리베이스로 커밋 시각이 미래일 수 있다
        if (elapsed.TotalMinutes < 1) return LocalizationService.Get("GitRelative_JustNow");

        if (elapsed.TotalHours < 1)
        {
            var minutes = (int)elapsed.TotalMinutes;
            return Format(minutes, "GitRelative_MinuteFormat", "GitRelative_MinutesFormat");
        }

        if (elapsed.TotalDays < 1)
        {
            var hours = (int)elapsed.TotalHours;
            return Format(hours, "GitRelative_HourFormat", "GitRelative_HoursFormat");
        }

        if (elapsed.TotalDays <= 30)
        {
            var days = (int)elapsed.TotalDays;
            return Format(days, "GitRelative_DayFormat", "GitRelative_DaysFormat");
        }

        return date.LocalDateTime.ToString("yyyy-MM-dd");

        // 영어는 1과 2 이상의 문구가 달라 단수·복수 키를 나눠 등록했다
        static string Format(int value, string singularKey, string pluralKey)
            => string.Format(LocalizationService.Get(value == 1 ? singularKey : pluralKey), value);
    }

    /// <summary>작성자명 첫 글자 — 아바타 원에 표시합니다.</summary>
    public static string AvatarInitial(string authorName)
        => string.IsNullOrWhiteSpace(authorName) ? "?" : authorName.Trim()[..1].ToUpperInvariant();

    /// <summary>작성자 이메일로 아바타 색을 고정합니다 — 같은 사람은 항상 같은 색이 됩니다.</summary>
    /// <remarks>
    /// 팔레트에 이미 있는 4색만 돌려 쓴다(신규 색 리터럴을 추가하지 않는다).
    /// 로컬 git으로는 실제 프로필 이미지를 얻을 수 없어 이니셜 원형으로 대체한 것이다.
    /// </remarks>
    public static Brush? AvatarBrush(string authorEmail)
    {
        if (_avatarBrushes is null) return null;

        var hash = 0;
        foreach (var c in authorEmail ?? string.Empty)
            hash = unchecked(hash * 31 + c);

        return _avatarBrushes[(int)((uint)hash % (uint)_avatarBrushes.Length)];
    }
}

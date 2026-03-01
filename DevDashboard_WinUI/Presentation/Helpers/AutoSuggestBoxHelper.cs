using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DevDashboard.Presentation.Helpers;

/// <summary>
/// AutoSuggestBox에 MaxLength 기능을 제공하는 Attached Property.
/// WinUI 3의 AutoSuggestBox에는 MaxLength 속성이 없으므로 이 헬퍼로 문자 수를 제한합니다.
/// 내부 TextBox에 직접 MaxLength를 설정하며, Loaded 이전에는 TextChanged로 보완합니다.
/// <para>사용 예: <c>helpers:AutoSuggestBoxHelper.MaxLength="50"</c></para>
/// </summary>
public static class AutoSuggestBoxHelper
{
    /// <summary>
    /// AutoSuggestBox의 최대 입력 문자 수를 제한합니다. 0이면 제한 없음(기본값).
    /// </summary>
    public static readonly DependencyProperty MaxLengthProperty =
        DependencyProperty.RegisterAttached(
            "MaxLength",
            typeof(int),
            typeof(AutoSuggestBoxHelper),
            new PropertyMetadata(0, OnMaxLengthChanged));

    public static int GetMaxLength(DependencyObject obj) => (int)obj.GetValue(MaxLengthProperty);
    public static void SetMaxLength(DependencyObject obj, int value) => obj.SetValue(MaxLengthProperty, value);

    private static void OnMaxLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not AutoSuggestBox asb)
            return;

        // 이전 핸들러 제거 (중복 등록 방지)
        asb.Loaded -= OnLoaded;
        asb.TextChanged -= OnTextChanged;

        var maxLength = (int)e.NewValue;
        if (maxLength <= 0)
            return;

        // 이미 로드된 경우 내부 TextBox에 즉시 적용, 아니면 Loaded 대기
        if (asb.IsLoaded)
            ApplyToInnerTextBox(asb, maxLength);
        else
            asb.Loaded += OnLoaded;

        // TextChanged로 붙여넣기 등 보완
        asb.TextChanged += OnTextChanged;
    }

    /// <summary>컨트롤 로드 완료 시 내부 TextBox에 MaxLength를 적용합니다.</summary>
    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        var asb = (AutoSuggestBox)sender;
        asb.Loaded -= OnLoaded;
        ApplyToInnerTextBox(asb, GetMaxLength(asb));
    }

    /// <summary>VisualTree를 탐색하여 AutoSuggestBox 내부 TextBox에 MaxLength를 설정합니다.</summary>
    private static void ApplyToInnerTextBox(AutoSuggestBox asb, int maxLength)
    {
        if (FindChild<TextBox>(asb) is { } tb)
            tb.MaxLength = maxLength;
    }

    /// <summary>붙여넣기 등으로 MaxLength를 초과한 경우 잘라냅니다.</summary>
    private static void OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        var maxLength = GetMaxLength(sender);
        if (maxLength <= 0 || sender.Text.Length <= maxLength)
            return;

        // 내부 TextBox가 처리하지 못한 경우(Loaded 전 붙여넣기 등) 보완 처리
        sender.TextChanged -= OnTextChanged;
        sender.Text = sender.Text[..maxLength];
        sender.TextChanged += OnTextChanged;
    }

    /// <summary>VisualTree를 재귀 탐색하여 지정 타입의 첫 번째 자식을 반환합니다.</summary>
    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found)
                return found;

            var result = FindChild<T>(child);
            if (result is not null)
                return result;
        }
        return null;
    }
}

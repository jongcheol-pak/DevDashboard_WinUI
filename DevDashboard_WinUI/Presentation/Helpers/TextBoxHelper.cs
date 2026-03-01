using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace DevDashboard.Presentation.Helpers;

/// <summary>
/// TextBox에 MaxLines 기능을 제공하는 Attached Property.
/// WinUI 3의 TextBox에는 MaxLines 속성이 없으므로 이 헬퍼로 줄 수를 제한합니다.
/// <para>사용 예: <c>helpers:TextBoxHelper.MaxLines="5"</c></para>
/// </summary>
public static class TextBoxHelper
{
    /// <summary>
    /// TextBox의 최대 입력 줄 수를 제한합니다. 0이면 제한 없음(기본값).
    /// </summary>
    public static readonly DependencyProperty MaxLinesProperty =
        DependencyProperty.RegisterAttached(
            "MaxLines",
            typeof(int),
            typeof(TextBoxHelper),
            new PropertyMetadata(0, OnMaxLinesChanged));

    public static int GetMaxLines(DependencyObject obj) => (int)obj.GetValue(MaxLinesProperty);
    public static void SetMaxLines(DependencyObject obj, int value) => obj.SetValue(MaxLinesProperty, value);

    private static void OnMaxLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
            return;

        // 이전 핸들러 제거 (중복 등록 방지)
        textBox.TextChanged -= OnTextChanged;
        textBox.KeyDown -= OnKeyDown;

        var maxLines = (int)e.NewValue;
        if (maxLines > 0)
        {
            textBox.TextChanged += OnTextChanged;
            textBox.KeyDown += OnKeyDown;
        }
    }

    /// <summary>
    /// Enter 키 입력 시 줄 수가 최대에 도달하면 입력을 차단합니다.
    /// </summary>
    private static void OnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter)
            return;

        var textBox = (TextBox)sender;
        var maxLines = GetMaxLines(textBox);

        if (maxLines > 0 && CountLines(textBox.Text) >= maxLines)
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// 붙여넣기 등으로 줄 수가 초과된 경우 초과분을 잘라냅니다.
    /// </summary>
    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = (TextBox)sender;
        var maxLines = GetMaxLines(textBox);

        if (maxLines <= 0)
            return;

        var lines = textBox.Text.Split('\r');
        if (lines.Length <= maxLines)
            return;

        // 초과 줄 제거
        textBox.TextChanged -= OnTextChanged;
        textBox.Text = string.Join('\r', lines.AsSpan(0, maxLines));
        textBox.SelectionStart = textBox.Text.Length;
        textBox.TextChanged += OnTextChanged;
    }

    private static int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 1;

        var count = 1;
        foreach (var ch in text)
        {
            if (ch == '\r')
                count++;
        }
        return count;
    }
}
